using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.Messages.Helper;

namespace Benzene.Xml;

public class XmlSerializationResponseHandler<TContext> : ISerializationResponseHandler<TContext> where TContext : class
{
    private readonly string _contentType = Settings.ContentTypeKey;
    private readonly string _applicationXml = Settings.ContentTypeValue;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly XmlSerializer _xmlSerializer;

    public XmlSerializationResponseHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter, IMessageHeadersGetter<TContext> messageHeadersGetter, XmlSerializer xmlSerializer)
    {
        _messageHeadersGetter = messageHeadersGetter;
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _xmlSerializer = xmlSerializer;
    }

    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult, IBodySerializer bodySerializer)
    {
        var headers = _messageHeadersGetter.GetHeaders(context);
        if (headers == null || !headers.TryGetValue(_contentType, out var contentTypeValue) ||
            !MediaType.Matches(contentTypeValue, _applicationXml))
        {
            return;
        }

        _benzeneResponseAdapter.SetBody(context, bodySerializer.Serialize(_xmlSerializer, messageHandlerResult));
        _benzeneResponseAdapter.SetContentType(context, _applicationXml);
    }
}