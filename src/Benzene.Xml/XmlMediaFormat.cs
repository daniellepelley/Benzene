using Benzene.Abstractions.DI;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.MediaFormats;

namespace Benzene.Xml;

/// <summary>
/// XML <see cref="Benzene.Abstractions.MessageHandlers.MediaFormats.IMediaFormat{TContext}"/>: selected
/// to read a request when its <c>content-type</c> is <c>application/xml</c>, and to write a response
/// when <c>application/xml</c> appears in its <c>accept</c> header.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this format applies to.</typeparam>
public class XmlMediaFormat<TContext> : AcceptHeaderMediaFormatBase<TContext>
{
    private readonly XmlSerializer _xmlSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlMediaFormat{TContext}"/> class.
    /// </summary>
    /// <param name="xmlSerializer">The shared XML serializer this format wraps.</param>
    public XmlMediaFormat(XmlSerializer xmlSerializer)
    {
        _xmlSerializer = xmlSerializer;
    }

    /// <inheritdoc />
    public override string ContentType => Constants.XmlContentType;

    /// <inheritdoc />
    public override ISerializer GetSerializer(IServiceResolver serviceResolver) => _xmlSerializer;
}
