using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.Response;

namespace Benzene.Xml;

public class XmlResponseHandler<TContext> : ResponseHandler<XmlSerializationResponseHandler<TContext>, TContext>
    where TContext : class, IHasMessageResult
{
    public XmlResponseHandler(XmlSerializationResponseHandler<TContext> httpSerializationResponseHandler, IResponsePayloadMapper<TContext> responsePayloadMapper) : base(httpSerializationResponseHandler, responsePayloadMapper)
    { }
}