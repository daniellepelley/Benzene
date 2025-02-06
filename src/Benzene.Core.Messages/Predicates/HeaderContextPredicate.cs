using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.Messages.Helper;

namespace Benzene.Core.Messages.Predicates;

public class HeaderContextPredicate<TContext> : IContextPredicate<TContext>
{
    private readonly string _headerKey;
    private readonly string _headerValue;

    public HeaderContextPredicate(string headerKey, string headerValue)
    {
        _headerValue = headerValue;
        _headerKey = headerKey;
    }

    public bool Check(TContext context, IServiceResolver serviceResolver)
    {
        var messageHeadersMapper = serviceResolver.GetService<IMessageHeadersGetter<TContext>>();
        if (DictionaryUtils.KeyEquals(messageHeadersMapper.GetHeaders(context), _headerKey,
                _headerValue))
        {
            return true;
        }

        return false;
    }
}