using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Helper;
using Benzene.Core.Request;

namespace Benzene.Xml;

public class XmlSerializerOption<TContext> : SerializerOptionBase<TContext, XmlSerializer>
{
    public override ISerializer GetSerializer(IServiceResolver serviceResolver)
    {
        return new XmlSerializer();
    }

    public override IContextPredicate<TContext> CanHandle => new XmlContentTypeHeaderContextPredicate<TContext>();
}