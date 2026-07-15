namespace Benzene.Abstractions.Messages.Mappers;

/// <summary>
/// Optional byte-oriented companion to <see cref="IMessageBodyGetter{TContext}"/>: exposes the raw
/// message body as bytes for transports that already hold it that way, so an
/// <see cref="Benzene.Abstractions.Serialization.IPayloadSerializer"/> can deserialize without an
/// intermediate string allocation. Registering this for a context type is what opts that transport
/// into the byte-oriented request-mapping path - unregistered, mapping falls back to
/// <see cref="IMessageBodyGetter{TContext}"/> unchanged.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this applies to.</typeparam>
public interface IMessageBodyBytesGetter<TContext>
{
    /// <summary>Gets the request's raw body as bytes.</summary>
    /// <param name="context">The context to extract the body from.</param>
    /// <returns>The request's raw body bytes, or empty if there is no body.</returns>
    ReadOnlyMemory<byte> GetBodyBytes(TContext context);
}
