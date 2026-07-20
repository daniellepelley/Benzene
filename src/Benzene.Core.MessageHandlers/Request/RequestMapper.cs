using System;
using System.Text;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Messages;

namespace Benzene.Core.MessageHandlers.Request;

/// <summary>
/// Default single-serializer <see cref="IRequestMapper{TContext}"/> implementation: deserializes the
/// message body via the given <see cref="ISerializer"/>, or reads it directly from
/// <see cref="IRequestContext{TRequest}"/> if the context already carries a pre-mapped request.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
public class RequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly ISerializer _serializer;
    private readonly IMessageBodyBytesGetter<TContext>? _messageBodyBytesGetter;
    private readonly IPayloadSerializer? _payloadSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestMapper{TContext}"/> class.
    /// </summary>
    /// <param name="messageBodyGetter">Extracts the raw message body from the context.</param>
    /// <param name="serializer">Deserializes the raw body into the requested type.</param>
    /// <param name="messageBodyBytesGetter">
    /// Optional byte-oriented companion to <paramref name="messageBodyGetter"/>. When this is
    /// non-null and <paramref name="serializer"/> implements <see cref="IPayloadSerializer"/>,
    /// <see cref="GetBody{TRequest}"/> deserializes directly from bytes instead of via an
    /// intermediate string.
    /// </param>
    public RequestMapper(IMessageBodyGetter<TContext> messageBodyGetter, ISerializer serializer,
        IMessageBodyBytesGetter<TContext>? messageBodyBytesGetter = null)
    {
        _serializer = serializer;
        _messageBodyGetter = messageBodyGetter;
        _messageBodyBytesGetter = messageBodyBytesGetter;
        _payloadSerializer = serializer as IPayloadSerializer;
    }

    /// <summary>
    /// Maps the context into the requested type: returns <see cref="IRequestContext{TRequest}.Request"/>
    /// directly if the context already implements <see cref="IRequestContext{TRequest}"/> for
    /// <typeparamref name="TRequest"/>; otherwise deserializes the raw message body, or returns a new
    /// default instance of <typeparamref name="TRequest"/> if the body is empty.
    /// </summary>
    /// <remarks>
    /// The empty-body fallback is intentional and load-bearing, not just a convenience default: bodyless
    /// requests (e.g. an HTTP GET) have no serialized payload to deserialize at all, and rely entirely on
    /// <see cref="Benzene.Core.MessageHandlers.Request.EnrichingRequestMapper{TContext}"/> (route
    /// parameters, query string, etc. via <see cref="IRequestEnricher{TContext}"/>) to populate the
    /// resulting instance's properties afterwards. Returning <c>null</c> here instead would skip
    /// enrichment entirely, since <see cref="Benzene.Core.MessageHandlers.Request.EnrichingRequestMapper{TContext}"/>
    /// treats a <c>null</c> inner-mapper result as "nothing to enrich".
    /// </remarks>
    /// <typeparam name="TRequest">The request type to map the body into.</typeparam>
    /// <param name="context">The transport-specific context for the incoming message.</param>
    /// <returns>The mapped request.</returns>
    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        if (context is IRequestContext<TRequest> requestContext)
        {
            return requestContext.Request;
        }

        // Raw-bytes passthrough (binary request bodies): a handler whose request type is
        // RawBytesRequest (or IRawBytesRequest) receives the request body's raw bytes verbatim - no
        // deserialization, no lossy UTF-8 round-trip. Prefer the byte getter (e.g. API Gateway's
        // base64 decode); fall back to the UTF-8 bytes of the string body for transports that only
        // expose a string getter. Checked before the serialized paths below, which would otherwise
        // try to JSON-deserialize the body into the raw-bytes type.
        if (typeof(TRequest) == typeof(RawBytesRequest) || typeof(TRequest) == typeof(IRawBytesRequest))
        {
            var raw = _messageBodyBytesGetter != null
                ? _messageBodyBytesGetter.GetBodyBytes(context)
                : AsUtf8Bytes(_messageBodyGetter.GetBody(context));

            return (TRequest)(object)new RawBytesRequest(raw);
        }

        if (_payloadSerializer != null && _messageBodyBytesGetter != null)
        {
            var bodyAsBytes = _messageBodyBytesGetter.GetBodyBytes(context);

            return !bodyAsBytes.IsEmpty
                ? (TRequest?)_payloadSerializer.Deserialize(typeof(TRequest), bodyAsBytes.Span)
                : Activator.CreateInstance<TRequest>();
        }

        var bodyAsString = _messageBodyGetter.GetBody(context);

        return !string.IsNullOrEmpty(bodyAsString)
            ? _serializer.Deserialize<TRequest>(bodyAsString)
            : Activator.CreateInstance<TRequest>();
    }

    private static ReadOnlyMemory<byte> AsUtf8Bytes(string? body)
    {
        return string.IsNullOrEmpty(body) ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(body);
    }
}
