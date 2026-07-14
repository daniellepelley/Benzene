using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Messages.Predicates;

/// <summary>
/// Matches a context whose <see cref="IMessageHeadersGetter{TContext}"/>-exposed headers contain the
/// configured header name with exactly the configured value. Subclasses can override
/// <see cref="IsMatch"/> to change how the header's actual value is compared against the expected
/// one (e.g. <see cref="MediaTypeHeaderContextPredicate{TContext}"/> for parameter/case-tolerant
/// media-type matching) without duplicating the header lookup itself.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this predicate can apply to.</typeparam>
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
        var headers = messageHeadersMapper.GetHeaders(context);

        return headers != null
               && headers.TryGetValue(_headerKey, out var actualValue)
               && IsMatch(actualValue, _headerValue);
    }

    /// <summary>
    /// Compares the header's actual value against the expected value. The default is exact string
    /// equality, preserving this class's original behavior for non-media-type headers.
    /// </summary>
    /// <param name="actualValue">The header's actual value on the context.</param>
    /// <param name="expectedValue">The value this predicate was constructed to match.</param>
    /// <returns><c>true</c> if the values match; otherwise <c>false</c>.</returns>
    protected virtual bool IsMatch(string actualValue, string expectedValue)
    {
        return actualValue == expectedValue;
    }
}