using Benzene.Abstractions.DI;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.MediaFormats;

namespace Benzene.MessagePack;

/// <summary>
/// MessagePack <see cref="Benzene.Abstractions.MessageHandlers.MediaFormats.IMediaFormat{TContext}"/>:
/// selected to read a request when its <c>content-type</c> is <c>application/msgpack</c>, and to
/// write a response when <c>application/msgpack</c> appears in its <c>accept</c> header.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this format applies to.</typeparam>
public class MessagePackMediaFormat<TContext> : AcceptHeaderMediaFormatBase<TContext>
{
    private readonly MessagePackSerializer _messagePackSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackMediaFormat{TContext}"/> class.
    /// </summary>
    /// <param name="messagePackSerializer">The shared MessagePack serializer this format wraps.</param>
    public MessagePackMediaFormat(MessagePackSerializer messagePackSerializer)
    {
        _messagePackSerializer = messagePackSerializer;
    }

    /// <inheritdoc />
    public override string ContentType => Constants.MessagePackContentType;

    /// <inheritdoc />
    public override ISerializer GetSerializer(IServiceResolver serviceResolver) => _messagePackSerializer;
}
