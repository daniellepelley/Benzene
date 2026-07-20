using System;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.Messages;

/// <summary>
/// Default <see cref="IRawBytesMessage"/>: wraps raw response bytes and their content type so a
/// handler can return a binary body (image, PDF, zip, …) that the response renderer writes verbatim.
/// </summary>
public class RawBytesMessage : IRawBytesMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawBytesMessage"/> class.
    /// </summary>
    /// <param name="content">The raw bytes to deliver as the response body.</param>
    /// <param name="contentType">The content type to deliver the body with (e.g. <c>image/png</c>).</param>
    public RawBytesMessage(ReadOnlyMemory<byte> content, string contentType)
    {
        Content = content;
        ContentType = contentType;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Content { get; }

    /// <inheritdoc />
    public string ContentType { get; }
}
