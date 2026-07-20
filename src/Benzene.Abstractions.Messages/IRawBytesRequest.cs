using System;

namespace Benzene.Abstractions.Messages;

/// <summary>
/// A request payload that carries the incoming message body's raw bytes verbatim, so a handler can
/// receive a binary upload (an image, a PDF, a zip, …) byte-identical instead of a deserialized
/// object. Declare a handler's request type as <see cref="IRawBytesRequest"/> (or the concrete
/// <c>Benzene.Core.Messages.RawBytesRequest</c>) and the request mapper skips deserialization,
/// handing over the raw body bytes — decoded from base64 first on transports that base64-encode
/// binary bodies (e.g. API Gateway).
/// </summary>
/// <remarks>
/// The request-side counterpart of <see cref="IRawBytesMessage"/> (the binary response payload).
/// </remarks>
public interface IRawBytesRequest
{
    /// <summary>The raw bytes of the request body, exactly as received.</summary>
    ReadOnlyMemory<byte> Content { get; }
}
