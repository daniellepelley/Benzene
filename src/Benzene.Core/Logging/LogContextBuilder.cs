using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MiddlewareBuilder;

namespace Benzene.Core.Logging;

public class LogContextBuilder<TContext> : IRegisterDependency 
{
    private readonly ContextDictionaryBuilder<TContext> _requestContextDictionaryBuilder = new();
    private readonly ContextDictionaryBuilder<TContext> _responseContextDictionaryBuilder = new();
    private readonly IRegisterDependency _registerDependency;

    public LogContextBuilder(IRegisterDependency registerDependency)
    {
        _registerDependency = registerDependency;
    }
    
    public LogContextBuilder<TContext> OnRequest(string key, string value)
    {
        _requestContextDictionaryBuilder.With(key, value);
        return this;
    }

    public LogContextBuilder<TContext> OnRequest(string key, Func<IServiceResolver, string> valueAction)
    {
        _requestContextDictionaryBuilder.With(key, valueAction);
        return this;
    }

    public LogContextBuilder<TContext> OnRequest(string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        _requestContextDictionaryBuilder.With(key, valueAction);
        return this;
    }
    
    public LogContextBuilder<TContext> OnRequest(IDictionary<string, string> dictionary)
    {
        _requestContextDictionaryBuilder.With(dictionary);
        return this;
    }

    public LogContextBuilder<TContext> OnRequest(Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        _requestContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    public LogContextBuilder<TContext> OnRequest(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
    {
        _requestContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    public LogContextBuilder<TContext> OnResponse(string key, string value)
    {
        _responseContextDictionaryBuilder.With(key, value);
        return this;
    }

    public LogContextBuilder<TContext> OnResponse(string key, Func<IServiceResolver, string> valueAction)
    {
        _responseContextDictionaryBuilder.With(key, valueAction);
        return this;
    }

    public LogContextBuilder<TContext> OnResponse(string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        _responseContextDictionaryBuilder.With(key, valueAction);
        return this;
    }

    public LogContextBuilder<TContext> OnResponse(IDictionary<string, string> dictionary)
    {
        _responseContextDictionaryBuilder.With(dictionary);
        return this;
    }

    public LogContextBuilder<TContext> OnResponse(Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        _responseContextDictionaryBuilder.With(dictionaryAction);
        return this;
    }

    public LogContextBuilder<TContext> OnResponse(Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction)
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
