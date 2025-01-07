using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Helper;
using Benzene.Core.Response;

namespace Benzene.Xml;

public class XmlSerializationResponseHandler<TContext> : ISerializationResponseHandler<TContext> where TContext : class 
{
    private readonly string _contentType = Settings.ContentTypeKey;
    private readonly string _applicationXml = Settings.ContentTypeValue;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;
    private readonly IMessageHeadersMapper<TContext> _messageHeadersMapper;

    public XmlSerializationResponseHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter, IMessageHeadersMapper<TContext> messageHeadersMapper)
    {
        _messageHeadersMapper = messageHeadersMapper;
        _benzeneResponseAdapter = benzeneResponseAdapter;
    }

    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult, IBodySerializer bodySerializer)
    {
        if (!DictionaryUtils.KeyEquals(_messageHeadersMapper.GetHeaders(context), _contentType,
                _applicationXml))
        {
            return;
        }

        _benzeneResponseAdapter.SetBody(context, bodySerializer.Serialize(new XmlSerializer(), messageHandlerResult));
        _benzeneResponseAdapter.SetContentType(context, _applicationXml);
    }
}