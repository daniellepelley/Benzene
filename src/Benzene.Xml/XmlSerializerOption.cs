using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Request;

namespace Benzene.Xml;

public class XmlSerializerOption<TContext> : SerializerOptionBase<TContext, XmlSerializer>
{
    public override ISerializer GetSerializer(IServiceResolver serviceResolver)
    {
        return new XmlSerializer();
    }

    public override IContextPredicate<TContext> CanHandle => new XmlContentTypeHeaderContextPredicate<TContext>();
}