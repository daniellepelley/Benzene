using Benzene.Abstractions.MessageHandlers.Request;

namespace Benzene.Core.Predicates;

public class ContextPredicateBuilder<TContext>
{
    public IContextPredicate<TContext> CheckHeader(string headerKey, string headerValue)
        => new HeaderContextPredicate<TContext>(headerKey, headerValue);

    public IContextPredicate<TContext> Always()
        => new InlineContextPredicate<TContext>((_, _) => true);
}