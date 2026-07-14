namespace Benzene.Abstractions.MessageHandlers.Request;

/// <summary>
/// Supplies out-of-band values (e.g. from route parameters, headers, or auth claims) to be applied
/// onto the already-deserialized request, so request properties that don't come from the message
/// body can still be populated. An <c>EnrichingRequestMapper{TContext}</c> maps the returned
/// dictionary's entries onto the request's properties by matching key to property name
/// (case-insensitively), overwriting any existing value on a match.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the enrichment is derived from.</typeparam>
public interface IRequestEnricher<TContext>
{
    /// <summary>Derives additional values to apply onto the deserialized request.</summary>
    /// <typeparam name="TRequest">The strongly-typed request being enriched.</typeparam>
    /// <param name="request">The already-deserialized request for the current invocation.</param>
    /// <param name="context">The transport-specific context for the current invocation.</param>
    /// <returns>
    /// Key/value pairs to apply onto <paramref name="request"/>'s properties, keyed by
    /// (case-insensitive) property name.
    /// </returns>
    IDictionary<string, object> Enrich<TRequest>(TRequest request, TContext context);
}