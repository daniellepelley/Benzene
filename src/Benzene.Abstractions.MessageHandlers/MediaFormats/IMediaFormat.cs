using Benzene.Abstractions.DI;
using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.MessageHandlers.MediaFormats;

/// <summary>
/// A single wire format (e.g. JSON, XML) available for both reading requests and writing responses,
/// replacing the pre-Phase-2 split between <c>ISerializerOption{TContext}</c> (request-only) and
/// <c>ISerializationResponseHandler{TContext}</c> (response-only) — one registration now drives both
/// directions of content negotiation via <see cref="IMediaFormatNegotiator{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this format can apply to.</typeparam>
public interface IMediaFormat<TContext>
{
    /// <summary>The media type this format produces on the response side (e.g. <c>"application/xml"</c>).</summary>
    string ContentType { get; }

    /// <summary>
    /// Whether this format should be used to deserialize the incoming request body for
    /// <paramref name="context"/> — typically decided from the request's <c>content-type</c> header.
    /// </summary>
    /// <param name="context">The transport-specific context for the current message.</param>
    /// <param name="serviceResolver">Resolver for any services needed to decide applicability.</param>
    bool CanRead(TContext context, IServiceResolver serviceResolver);

    /// <summary>
    /// Whether this format should be used to serialize the outgoing response for
    /// <paramref name="context"/> — typically decided from the request's <c>accept</c> header.
    /// </summary>
    /// <param name="context">The transport-specific context for the current message.</param>
    /// <param name="serviceResolver">Resolver for any services needed to decide applicability.</param>
    bool CanWrite(TContext context, IServiceResolver serviceResolver);

    /// <summary>Resolves the serializer this format uses to read/write its wire representation.</summary>
    /// <param name="serviceResolver">Resolver used to obtain the serializer instance.</param>
    ISerializer GetSerializer(IServiceResolver serviceResolver);
}
