using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.Helper;
using Benzene.Core.Response;

namespace Benzene.Xml;

public class XmlSerializationResponseHandler<TContext> : ISerializationResponseHandler<TContext> where TContext : class, IHasMessageResult
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

    public void HandleAsync(TContext context, IBodySerializer bodySerializer)
    {
        if (!DictionaryUtils.KeyEquals(_messageHeadersMapper.GetHeaders(context), _contentType,
                _applicationXml))
        {
            return;
        }

        _benzeneResponseAdapter.SetBody(context, bodySerializer.Serialize(new XmlSerializer()));
        _benzeneResponseAdapter.SetResponseHeader(context, _contentType, _applicationXml);
    }
}