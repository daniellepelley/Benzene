using Benzene.Abstractions.DI;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.MediaFormats;

namespace Benzene.Avro;

/// <summary>
/// Avro <see cref="Benzene.Abstractions.MessageHandlers.MediaFormats.IMediaFormat{TContext}"/>:
/// selected to read a request when its <c>content-type</c> is <c>application/avro</c>, and to write a
/// response when <c>application/avro</c> appears in its <c>accept</c> header.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this format applies to.</typeparam>
public class AvroMediaFormat<TContext> : AcceptHeaderMediaFormatBase<TContext>
{
    private readonly AvroSerializer _avroSerializer;

    /// <summary>Initializes a new instance of the <see cref="AvroMediaFormat{TContext}"/> class.</summary>
    /// <param name="avroSerializer">The shared Avro serializer this format wraps.</param>
    public AvroMediaFormat(AvroSerializer avroSerializer)
    {
        _avroSerializer = avroSerializer;
    }

    /// <inheritdoc />
    public override string ContentType => Constants.AvroContentType;

    /// <inheritdoc />
    public override ISerializer GetSerializer(IServiceResolver serviceResolver) => _avroSerializer;
}
