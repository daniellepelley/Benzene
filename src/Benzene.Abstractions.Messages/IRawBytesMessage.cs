using System;

namespace Benzene.Abstractions.Messages;

/// <summary>
/// A raw binary payload that carries its own content type, letting a handler return bytes verbatim
/// (an image, a PDF, a zip, …) instead of a serialized object. The response renderer writes
/// <see cref="Content"/> straight to the transport's body via the byte-oriented
/// <c>IBenzeneResponseAdapter.SetBody(TContext, ReadOnlyMemory&lt;byte&gt;)</c> overload — base64-encoding
/// and flagging the response where the transport requires it (e.g. API Gateway's
/// <c>IsBase64Encoded</c>) — bypassing serialization and format negotiation entirely.
/// </summary>
/// <remarks>
/// The binary counterpart of <see cref="IRawStringMessage"/>/<see cref="IRawContentMessage"/>: those
/// deliver pre-rendered <em>text</em> (with an optional content type); this delivers raw <em>bytes</em>.
/// </remarks>
public interface IRawBytesMessage
{
    /// <summary>The raw bytes to deliver as the response body, unmodified.</summary>
    ReadOnlyMemory<byte> Content { get; }

    /// <summary>The content type this payload should be delivered with (e.g. <c>image/png</c>).</summary>
    string ContentType { get; }
}
