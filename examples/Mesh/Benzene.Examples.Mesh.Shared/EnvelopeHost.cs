using System.Text;
using System.Text.Json;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Wire;
using Benzene.Microsoft.Dependencies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.Mesh.Shared;

/// <summary>
/// Hosts a set of message handlers behind a wire-envelope HTTP endpoint
/// (<c>POST {topic, headers, body}</c> in, <c>{statusCode, headers, body}</c> out) - the
/// service-to-service surface the mesh speaks. When given a <see cref="MeshServiceInfo"/> and a
/// collector envelope URL, the pipeline is meshed per docs/specification/mesh.md: the reserved
/// <c>mesh</c> descriptor topic, per-invocation trace events pushed to the collector, and
/// <see cref="StartAnnouncing"/> for registration + heartbeats (log-and-continue - an unreachable
/// collector reduces the mesh, never the service).
/// </summary>
public sealed class EnvelopeHost
{
    private readonly BenzeneMessageApplication _application;
    private readonly Abstractions.DI.IServiceResolverFactory _resolverFactory;
    private readonly MeshServiceInfo? _meshInfo;
    private readonly MeshServiceDescriptor? _descriptor;
    private readonly string? _collectorEnvelopeUrl;
    private readonly Func<bool>? _healthy;
    private static readonly HttpClient Http = new();

    public EnvelopeHost(
        Type[] handlerTypes,
        Action<IServiceCollection>? configureServices = null,
        MeshServiceInfo? meshInfo = null,
        string? collectorEnvelopeUrl = null,
        Func<bool>? healthy = null)
    {
        _meshInfo = meshInfo;
        _collectorEnvelopeUrl = collectorEnvelopeUrl;
        _healthy = healthy;

        var services = new ServiceCollection();
        services.AddLogging();
        configureServices?.Invoke(services);

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage();

        var pipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        if (meshInfo != null && collectorEnvelopeUrl != null)
        {
            _descriptor = MeshDescriptorFactory.Create(
                new TypesLookUp(new ReflectionMessageHandlersFinder(handlerTypes).FindDefinitions()), meshInfo);
            pipelineBuilder.UseMeshTrace(meshInfo,
                new HttpMeshTraceExporter(Http, collectorEnvelopeUrl, batchSize: 8, flushInterval: TimeSpan.FromSeconds(1)),
                new BenzeneMessageMeshStatusReader());
            pipelineBuilder.UseMeshDescriptor(_descriptor);
        }
        pipelineBuilder.UseMessageHandlers(handlerTypes);

        _application = new BenzeneMessageApplication(pipelineBuilder.Build());
        _resolverFactory = container.CreateServiceResolverFactory();
    }

    /// <summary>Adapts an ASP.NET request to the envelope pipeline. Map it with
    /// <c>endpoints.MapPost("/invoke", host.HandleAsync)</c>.</summary>
    public async Task HandleAsync(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body);
        var envelope = JsonSerializer.Deserialize<Envelope>(await reader.ReadToEndAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Envelope();

        var response = await _application.HandleAsync(new BenzeneMessageRequest
        {
            Topic = envelope.Topic,
            Headers = envelope.Headers ?? new Dictionary<string, string>(),
            Body = envelope.Body ?? string.Empty
        }, _resolverFactory);

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            statusCode = response.StatusCode,
            headers = response.Headers ?? new Dictionary<string, string>(),
            body = response.Body ?? string.Empty
        }));
    }

    /// <summary>
    /// Registers with the collector (retrying until it's up) and then heartbeats every 10s,
    /// carrying <c>healthy()</c> and the descriptor hash. Fire-and-forget from startup; failures
    /// log to console and never affect the service.
    /// </summary>
    public void StartAnnouncing(CancellationToken cancellationToken = default)
    {
        if (_descriptor == null || _collectorEnvelopeUrl == null)
        {
            return;
        }
        _ = Task.Run(async () =>
        {
            for (var attempt = 0; attempt < 30 && !cancellationToken.IsCancellationRequested; attempt++)
            {
                if (await SendAsync(MeshTopics.Register, MeshJson.Serialize(_descriptor)))
                {
                    Console.WriteLine($"[mesh] {_meshInfo!.Service} registered with the collector");
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                await SendAsync(MeshTopics.Heartbeat, MeshJson.Serialize(new MeshHeartbeat
                {
                    Service = _meshInfo!.Service,
                    InstanceId = _meshInfo.InstanceId,
                    DescriptorHash = _descriptor.DescriptorHash,
                    SentAt = DateTimeOffset.UtcNow,
                    Health = new HealthCheckResponse(_healthy?.Invoke() ?? true, new Dictionary<string, HealthCheckResult>())
                }));
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task<bool> SendAsync(string topic, string body)
    {
        try
        {
            var envelope = JsonSerializer.Serialize(new { topic, headers = new Dictionary<string, string>(), body });
            using var content = new StringContent(envelope, Encoding.UTF8, "application/json");
            using var response = await Http.PostAsync(_collectorEnvelopeUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false; // collector down: reduced mesh, never a service failure
        }
    }

    private class Envelope
    {
        public string Topic { get; set; } = string.Empty;
        public Dictionary<string, string>? Headers { get; set; }
        public string? Body { get; set; }
    }

    private class TypesLookUp : IMessageHandlerDefinitionLookUp
    {
        private readonly IMessageHandlerDefinition[] _definitions;

        public TypesLookUp(IMessageHandlerDefinition[] definitions) => _definitions = definitions;

        public IMessageHandlerDefinition? FindHandler(ITopic topic) =>
            _definitions.FirstOrDefault(x => x.Topic.Id == topic.Id && x.Topic.Version == topic.Version);

        public IMessageHandlerDefinition[] GetAllHandlers() => _definitions;
    }
}

/// <summary>
/// A tiny envelope-speaking client for cross-service calls in the examples, forwarding the
/// current mesh span as a <c>traceparent</c> header so the collector can derive consumer edges
/// from parentage.
/// </summary>
public static class EnvelopeClient
{
    private static readonly HttpClient Http = new();

    public static async Task<(string StatusCode, string Body)> SendAsync(string envelopeUrl, string topic, string body)
    {
        var headers = new Dictionary<string, string>();
        if (MeshSpan.Current is { } span)
        {
            headers["traceparent"] = span.ToTraceparent();
        }
        var envelope = JsonSerializer.Serialize(new { topic, headers, body });
        using var content = new StringContent(envelope, Encoding.UTF8, "application/json");
        using var response = await Http.PostAsync(envelopeUrl, content);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (
            document.RootElement.GetProperty("statusCode").GetString() ?? string.Empty,
            document.RootElement.GetProperty("body").GetString() ?? string.Empty);
    }
}
