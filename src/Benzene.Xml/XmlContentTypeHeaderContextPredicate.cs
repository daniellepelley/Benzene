using Benzene.Core.Messages.Predicates;

namespace Benzene.Xml;

public class XmlContentTypeHeaderContextPredicate<TContext> : MediaTypeHeaderContextPredicate<TContext>
{
    public XmlContentTypeHeaderContextPredicate()
        : base(Settings.ContentTypeKey, Settings.ContentTypeValue)
    { }
}