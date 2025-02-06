using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Logging;

public class LogContextBuilder<TContext> : ILogContextBuilder<TContext>
{
    private readonly IContextDictionaryBuilder<TContext> _requestContextDictionaryBuilder = new ContextDictionaryBuilder<TContext>();
    private readonly IContextDictionaryBuilder<TContext> _responseContextDictionaryBuilder = new ContextDictionaryBuilder<TContext>();
    private readonly IRegisterDependency _registerDependency;

    public LogContextBuilder(IRegisterDependency registerDependency)
    {
        _registerDependency = registerDependency;
    }
    
    public ILogContextBuilder<TContext> OnRequest(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
    {
        _requestContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    public ILogContextBuilder<TContext> OnResponse(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
    {
        _responseContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }
    
    public IDisposable CreateForRequest(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver, TContext context)
    {
        return benzeneLogContext.Create(_requestContextDictionaryBuilder.Build(serviceResolver, context));
    }

    public IDisposable CreateForResponse(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver, TContext context)
    {
        return benzeneLogContext.Create(_responseContextDictionaryBuilder.Build(serviceResolver, context));
    }

    public void Register(Action<IBenzeneServiceContainer> action)
    {
        _registerDependency.Register(action);
    }
}
