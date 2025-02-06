using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Response;

namespace Benzene.Xml;

public class XmlSerializationResponseHandler<TContext> : ISerializationResponseHandler<TContext> where TContext : class 
{
    private readonly string _contentType = Settings.ContentTypeKey;
    private readonly string _applicationXml = Settings.ContentTypeValue;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;

    public XmlSerializationResponseHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter, IMessageHeadersGetter<TContext> messageHeadersGetter)
    {
        _messageHeadersGetter = messageHeadersGetter;
        _benzeneResponseAdapter = benzeneResponseAdapter;
    }

    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult, IBodySerializer bodySerializer)
    {
        if (!KeyEquals(_messageHeadersGetter.GetHeaders(context), _contentType,
                _applicationXml))
        {
            return;
        }

        _benzeneResponseAdapter.SetBody(context, bodySerializer.Serialize(new XmlSerializer(), messageHandlerResult));
        _benzeneResponseAdapter.SetContentType(context, _applicationXml);
    }

    public static bool KeyEquals(IDictionary<string, string> dictionary, string key, string value)
    {
        if (dictionary != null && dictionary.TryGetValue(key, out var keyValue))
        {
            return keyValue == value;
        }

        return false;
    }

}