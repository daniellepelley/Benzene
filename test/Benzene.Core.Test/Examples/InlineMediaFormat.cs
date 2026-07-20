using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.Serialization;

namespace Benzene.Test.Examples;

/// <summary>
/// Test helper: an <see cref="IMediaFormat{TContext}"/> built from inline delegates, so tests can
/// stand up a media format without declaring a dedicated class. (Formerly shipped in
/// <c>Benzene.Extras</c>; relocated here when that package was decommissioned, as tests were its
/// only consumers.)
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this format applies to.</typeparam>
public class InlineMediaFormat<TContext> : IMediaFormat<TContext>
{
    private readonly ISerializer _serializer;
    private readonly Func<TContext, IServiceResolver, bool> _canRead;
    private readonly Func<TContext, IServiceResolver, bool> _canWrite;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineMediaFormat{TContext}"/> class using the same
    /// predicate for both reading and writing.
    /// </summary>
    /// <param name="contentType">The format's content type.</param>
    /// <param name="serializer">The serializer used to read and write this format.</param>
    /// <param name="canHandle">Predicate deciding whether this format applies to the given context.</param>
    public InlineMediaFormat(string contentType, ISerializer serializer, Func<TContext, bool> canHandle)
        : this(contentType, serializer, (context, _) => canHandle(context))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineMediaFormat{TContext}"/> class using the same
    /// predicate for both reading and writing.
    /// </summary>
    /// <param name="contentType">The format's content type.</param>
    /// <param name="serializer">The serializer used to read and write this format.</param>
    /// <param name="canHandle">Predicate deciding whether this format applies to the given context.</param>
    public InlineMediaFormat(string contentType, ISerializer serializer, Func<TContext, IServiceResolver, bool> canHandle)
        : this(contentType, serializer, canHandle, canHandle)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineMediaFormat{TContext}"/> class using distinct
    /// predicates for reading and writing.
    /// </summary>
    /// <param name="contentType">The format's content type.</param>
    /// <param name="serializer">The serializer used to read and write this format.</param>
    /// <param name="canRead">Predicate deciding whether this format applies to reading the request body.</param>
    /// <param name="canWrite">Predicate deciding whether this format applies to writing the response body.</param>
    public InlineMediaFormat(string contentType, ISerializer serializer,
        Func<TContext, IServiceResolver, bool> canRead, Func<TContext, IServiceResolver, bool> canWrite)
    {
        ContentType = contentType;
        _serializer = serializer;
        _canRead = canRead;
        _canWrite = canWrite;
    }

    /// <inheritdoc />
    public string ContentType { get; }

    /// <inheritdoc />
    public bool CanRead(TContext context, IServiceResolver serviceResolver) => _canRead(context, serviceResolver);

    /// <inheritdoc />
    public bool CanWrite(TContext context, IServiceResolver serviceResolver) => _canWrite(context, serviceResolver);

    /// <inheritdoc />
    public ISerializer GetSerializer(IServiceResolver serviceResolver) => _serializer;
}
