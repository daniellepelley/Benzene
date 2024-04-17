using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Helper;
using Benzene.Core.Request;

namespace Benzene.Xml;

public class XmlSerializerOption<TContext> : SerializerOptionBase<TContext, XmlSerializer>
{
    private readonly string _contentType = Settings.ContentTypeKey;
    private readonly string _applicationXml = Settings.ContentTypeValue;
    
    private readonly IMessageHeadersMapper<TContext> _messageHeadersMapper;

    public XmlSerializerOption(IMessageHeadersMapper<TContext> messageHeadersMapper)
    {
        _messageHeadersMapper = messageHeadersMapper;
    }

    public override ISerializer GetSerializer(IServiceResolver serviceResolver)
    {
        return new XmlSerializer();
    }

    public override Func<TContext, bool> CanHandle => CanHandlerInner;

    private bool CanHandlerInner(TContext context)
    {
        if (DictionaryUtils.KeyEquals(_messageHeadersMapper.GetHeaders(context), _contentType,
                _applicationXml))
        {
            return true;
        }

        return false;
    }
}
