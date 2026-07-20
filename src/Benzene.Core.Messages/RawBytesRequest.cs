using System;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.Messages;

/// <summary>
/// Default <see cref="IRawBytesRequest"/>: wraps the incoming request body's raw bytes so a handler
/// can receive a binary upload verbatim. Produced by the request mapper when a handler declares its
/// request type as <see cref="RawBytesRequest"/> or <see cref="IRawBytesRequest"/>.
/// </summary>
public class RawBytesRequest : IRawBytesRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawBytesRequest"/> class.
    /// </summary>
    /// <param name="content">The raw bytes of the request body.</param>
    public RawBytesRequest(ReadOnlyMemory<byte> content)
    {
        Content = content;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Content { get; }
}
