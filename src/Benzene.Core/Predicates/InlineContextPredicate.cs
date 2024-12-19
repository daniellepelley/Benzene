﻿using System;
using System.Security.Cryptography.X509Certificates;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Request;
using Benzene.Core.BenzeneMessage;

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

public class ContextPredicateBuilder<TContext>
{
    public IContextPredicate<TContext> CheckHeader(string headerKey, string headerValue)
        => new HeaderContextPredicate<TContext>(headerKey, headerValue);

    public IContextPredicate<TContext> Always()
        => new InlineContextPredicate<TContext>((_, _) => true);
}