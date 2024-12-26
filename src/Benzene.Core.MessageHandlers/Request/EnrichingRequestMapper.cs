using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Helper;

namespace Benzene.Core.Request;

public class EnrichingRequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IEnumerable<IRequestEnricher<TContext>> _enrichers;
    private readonly IRequestMapper<TContext> _requestMapper;

    public EnrichingRequestMapper(IRequestMapper<TContext> requestMapper, IEnumerable<IRequestEnricher<TContext>> enrichers)
    {
        _enrichers = enrichers;
        _requestMapper = requestMapper;
    }

    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        var request = _requestMapper.GetBody<TRequest>(context);

        if (request == null)
        {
            return null;
        }

        var dictionary = _enrichers.Aggregate(new Dictionary<string, object>() as IDictionary<string, object>, (current, enricher) =>
            DictionaryUtils.MapOnto(current, enricher.Enrich(request, context)));

        return DictionaryUtils.Enrich(request, dictionary);
    }
}