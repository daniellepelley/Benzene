using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;

namespace Benzene.Core.Predicates;

public class InlineContextPredicate<TContext> : IContextPredicate<TContext>
{
    private readonly Func<TContext, IServiceResolver, bool> _canHandle;

    public InlineContextPredicate(Func<TContext, IServiceResolver, bool> canHandle)
    {
        _canHandle = canHandle;
    }

    public bool Check(TContext context, IServiceResolver serviceResolver)
    {
        return _canHandle(context, serviceResolver);
    }
}