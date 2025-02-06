using Benzene.Core.Messages.Predicates;

namespace Benzene.Xml;

public class XmlContentTypeHeaderContextPredicate<TContext> : HeaderContextPredicate<TContext>
{
    public XmlContentTypeHeaderContextPredicate()
        : base(Settings.ContentTypeKey, Settings.ContentTypeValue)
    { }
}