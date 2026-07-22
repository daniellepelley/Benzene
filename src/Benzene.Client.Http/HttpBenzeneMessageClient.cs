using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Clients.Common;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using JsonSerializer = Benzene.Clients.JsonSerializer;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Client.Http;

/// <summary>
/// A Benzene message client that carries a lightweight BenzeneMessage envelope over HTTP: it POSTs
/// <c>{ topic, headers, body }</c> to a target service's BenzeneMessage endpoint (the serving side's
/// <c>BenzeneMessageHttpMiddleware</c>, path <c>/benzene-message</c> by default) and maps the returned
/// <c>{ statusCode, headers, body }</c> envelope to a typed <see cref="IBenzeneResult{T}"/>. This is the
/// HTTP-transport counterpart of the direct AWS Lambda invoke path (<c>AwsLambdaBenzeneMessageClient</c>) —
/// the topic travels <em>inside</em> the JSON body, so one endpoint serves every topic, letting two Benzene
/// containers exchange transport-neutral messages over ordinary HTTP.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>HttpContextConverter</c>/<c>UseHttp(verb, path)</c> in this same package — which makes a plain
/// REST call, sending the raw message as the body to a verb+path and mapping the HTTP status — this client
/// speaks the BenzeneMessage <em>envelope</em>, so the receiving side routes on the envelope's topic exactly
/// as it would for a queue or a Lambda invoke.
/// </para>
/// <para>
/// The response envelope carries the authoritative Benzene status in its own body; the target maps that onto
/// the HTTP status too, so a mapped non-2xx (e.g. 404 for <c>NotFound</c>) is a normal result, not a transport
/// failure. The client therefore reads and maps the envelope regardless of the HTTP status code rather than
/// throwing on non-success. Only a genuine transport error (connection failure, a non-envelope error page)
/// surfaces as <see cref="BenzeneResult.ServiceUnavailable{T}"/>.
/// </para>
/// <para>
/// You supply the <see cref="HttpClient"/> (its lifetime and any <c>BaseAddress</c>/handler policy are yours
/// to own, as with the rest of this package). <paramref name="url"/> is used verbatim as the request URI, so
/// it may be absolute or relative to the client's <c>BaseAddress</c>.
/// </para>
/// </remarks>
public class HttpBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly ILogger? _logger;
    private readonly ISerializer _serializer;
    private readonly ICancellationTokenAccessor? _cancellation;

    /// <summary>Initializes a new instance of the <see cref="HttpBenzeneMessageClient"/> class.</summary>
    /// <param name="httpClient">The client used to POST the envelope. Its lifetime is the caller's responsibility.</param>
    /// <param name="url">The target BenzeneMessage endpoint URL (used verbatim as the request URI; absolute, or relative to the client's <c>BaseAddress</c>).</param>
    /// <param name="logger">Optional logger for invocation outcomes; null disables logging.</param>
    /// <param name="cancellation">Supplies the ambient cancellation token to pass to the request; null observes no cancellation.</param>
    public HttpBenzeneMessageClient(HttpClient httpClient, string url, ILogger? logger = null, ICancellationTokenAccessor? cancellation = null)
    {
        _httpClient = httpClient;
        _url = url;
        _logger = logger;
        _serializer = new JsonSerializer();
        _cancellation = cancellation;
    }

    /// <summary>Sends the request as a BenzeneMessage envelope over HTTP and maps the response envelope.</summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">
    /// The expected response payload type. When this is <see cref="Void"/> the response body is not
    /// deserialized — only the envelope's status is mapped (the send-acknowledgement shape).
    /// </typeparam>
    /// <param name="request">The client request to send.</param>
    /// <returns>The mapped result of the target's response envelope, or a service-unavailable result if the call threw.</returns>
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var envelope = new BenzeneMessageEnvelope
            {
                Topic = request.Topic,
                Headers = request.Headers,
                Body = _serializer.Serialize(request.Message)
            };

            using var content = new StringContent(_serializer.Serialize(envelope), Encoding.UTF8, "application/json");
            var token = _cancellation?.CancellationToken ?? CancellationToken.None;

            using var httpResponse = await _httpClient.PostAsync(_url, content, token);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(token);

            var clientResponse = string.IsNullOrWhiteSpace(responseBody)
                ? null
                : _serializer.Deserialize<BenzeneMessageClientResponse>(responseBody);

            if (clientResponse == null)
            {
                // An empty/blank body is not a BenzeneMessage envelope - the target returned nothing to map.
                _logger?.LogError("Message {receiverTopic} to {receiver} returned an empty response body", request.Topic, _url);
                return BenzeneResult.ServiceUnavailable<TResponse>($"Empty response from {_url}");
            }

            // For Void (send-acknowledgement) callers, map the status without deserializing a payload body:
            // a Void-returning handler's body shape is undefined, so feeding it to AsBenzeneResult<Void> could
            // throw. Nulling the body drives AsBenzeneResult down its status-only branch.
            var result = typeof(TResponse) == typeof(Void)
                ? new BenzeneMessageClientResponse(clientResponse.StatusCode, null!, clientResponse.Headers).AsBenzeneResult<TResponse>(_serializer)
                : clientResponse.AsBenzeneResult<TResponse>(_serializer);

            _logger?.LogInformation("Message {receiverTopic} sent to {receiver} with status {receiverStatus}",
                request.Topic, _url, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", request.Topic, _url);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <summary>Disposes the client. No-op; the injected <see cref="HttpClient"/> is owned by the caller/DI.</summary>
    public void Dispose()
    {
        // Method intentionally left empty - the HttpClient's lifetime is the caller's responsibility.
    }
}
