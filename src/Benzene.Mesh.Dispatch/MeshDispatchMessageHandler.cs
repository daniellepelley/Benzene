using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.Messages;
using Benzene.Mesh.Contracts;
using Benzene.Results;

namespace Benzene.Mesh.Dispatch;

/// <summary>
/// Serves the <c>mesh:dispatch</c> topic: invokes ONE registered service's real handler with a
/// caller-supplied payload and returns its response. Off unless <see cref="MeshDispatchGate.IsAllowed"/>
/// (opt-in registration AND non-Production / AllowInProduction) - a real handler runs, with real
/// side-effects. This is the direct-to-consumer F3b path; it reuses the same access the aggregator
/// already uses to interrogate each service (HTTP GET / Lambda Invoke), changing the payload, not the
/// permission, and is bounded to one declared service - never a shared queue.
/// </summary>
public class MeshDispatchMessageHandler : IMessageHandler<MeshDispatchRequest, RawStringMessage>
{
    private static readonly JsonSerializerOptions ResultJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly MeshDispatchGate _gate;
    private readonly MeshServiceRegistry _registry;
    private readonly IReadOnlyList<IMeshServiceDispatcher> _dispatchers;

    /// <summary>Initializes a new instance of the <see cref="MeshDispatchMessageHandler"/> class.</summary>
    public MeshDispatchMessageHandler(MeshDispatchGate gate, MeshServiceRegistry registry, IEnumerable<IMeshServiceDispatcher> dispatchers)
    {
        _gate = gate;
        _registry = registry;
        _dispatchers = dispatchers.ToArray();
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult<RawStringMessage>> HandleAsync(MeshDispatchRequest request)
    {
        if (!_gate.IsAllowed)
        {
            return BenzeneResult.Set<RawStringMessage>(BenzeneResultStatus.Forbidden, _gate.BlockedReason);
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Service) || string.IsNullOrWhiteSpace(request.Topic))
        {
            return BenzeneResult.BadRequest<RawStringMessage>("A dispatch request needs both a 'service' and a 'topic'.");
        }

        var entry = _registry.Services.FirstOrDefault(s => string.Equals(s.Name, request.Service, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return BenzeneResult.NotFound<RawStringMessage>($"No service named '{request.Service}' is registered in the mesh.");
        }

        var dispatcher = _dispatchers.FirstOrDefault(d => string.Equals(d.Key, entry.Source, StringComparison.OrdinalIgnoreCase));
        if (dispatcher == null)
        {
            return BenzeneResult.Set<RawStringMessage>(BenzeneResultStatus.NotImplemented,
                $"No dispatcher is registered for source '{entry.Source}' (service '{entry.Name}'). "
                + "Register the matching transport dispatcher (e.g. AddMeshLambdaDispatcher() for AwsLambdaInvoke).");
        }

        var envelope = new MeshDispatchEnvelope(
            request.Topic!,
            request.Headers ?? new Dictionary<string, string>(),
            request.Body ?? string.Empty);

        var result = await dispatcher.DispatchAsync(entry, envelope, CancellationToken.None);
        var json = JsonSerializer.Serialize(result, ResultJsonOptions);
        return BenzeneResult.Ok(new RawStringMessage(json));
    }
}
