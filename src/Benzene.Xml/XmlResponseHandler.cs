using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Response;

namespace Benzene.Xml;

public class XmlResponseHandler<TContext> : ResponseHandler<XmlSerializationResponseHandler<TContext>, TContext>
    where TContext : class
{
    public XmlResponseHandler(XmlSerializationResponseHandler<TContext> httpSerializationResponseHandler, IResponsePayloadMapper<TContext> responsePayloadMapper) : base(httpSerializationResponseHandler, responsePayloadMapper)
    { }
}