using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Helper;

namespace Benzene.Core.MessageHandlers.Request;

/// <summary>
/// Decorates another <see cref="IRequestMapper{TContext}"/>, applying every registered
/// <see cref="IRequestEnricher{TContext}"/> onto the mapped request afterwards, so out-of-band values
/// (route parameters, headers, claims, etc.) can populate request properties that don't come from the
/// message body.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
/// <remarks>
/// If the inner mapper returns <c>null</c>, enrichment is skipped and <c>null</c> is returned - there
/// is no request object to enrich. Enrichers are folded onto an accumulator dictionary in registration
/// order, and a fold step only fills in a key that is still missing or default - so it's <b>earlier</b>
/// enrichers that take precedence for a given property; a later enricher can only supply a value for a
/// property no earlier enricher has already set.
/// </remarks>
public class EnrichingRequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IRequestEnricher<TContext>[] _enrichers;
    private readonly IRequestMapper<TContext> _requestMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnrichingRequestMapper{TContext}"/> class.
    /// </summary>
    /// <param name="requestMapper">The inner mapper used to produce the base request.</param>
    /// <param name="enrichers">The enrichers to apply onto the mapped request, in order.</param>
    public EnrichingRequestMapper(IRequestMapper<TContext> requestMapper, IEnumerable<IRequestEnricher<TContext>> enrichers)
    {
        _enrichers = enrichers as IRequestEnricher<TContext>[] ?? enrichers.ToArray();
        _requestMapper = requestMapper;
    }

    /// <summary>
    /// Maps the context via the inner mapper, then applies every registered enricher's values onto
    /// the resulting request.
    /// </summary>
    /// <typeparam name="TRequest">The request type to map the body into.</typeparam>
    /// <param name="context">The transport-specific context for the incoming message.</param>
    /// <returns>The mapped and enriched request, or <c>null</c> if the inner mapper produced none.</returns>
    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        var request = _requestMapper.GetBody<TRequest>(context);

        if (request == null || _enrichers.Length == 0)
        {
            return request;
        }

        var dictionary = _enrichers.Aggregate(new Dictionary<string, object>() as IDictionary<string, object>, (current, enricher) =>
            DictionaryUtils.MapOnto(current, enricher.Enrich(request, context)));

        return DictionaryUtils.Enrich(request, dictionary);
    }
}
