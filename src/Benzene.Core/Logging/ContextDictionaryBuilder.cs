using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;

namespace Benzene.Core.Logging;

public class ContextDictionaryBuilder<TContext> 
{
    private readonly List<Func<IServiceResolver, TContext, IDictionary<string, string>>> _list = new();

    public ContextDictionaryBuilder<TContext> With(string key, string value)
    {
        Add(key, (_, _) => value);
        return this;
    }
    
    public ContextDictionaryBuilder<TContext> With(IDictionary<string, string> dictionary)
    {
        _list.Add((_, _) => dictionary);
        return this;
    }

    public ContextDictionaryBuilder<TContext> With(string key, Func<IServiceResolver, string> valueAction)
    {
        Add(key, (resolver, _) => valueAction(resolver));
        return this;
    }
    
    public ContextDictionaryBuilder<TContext> With(Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        _list.Add((resolver, _) => dictionaryAction(resolver));
        return this;
    }

    public ContextDictionaryBuilder<TContext> With(string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        Add(key, valueAction);
        return this;
    }
    
    public ContextDictionaryBuilder<TContext> With(Func<IServiceResolver, TContext,  IDictionary<string, string>> dictionaryAction)
    {
        _list.Add(dictionaryAction);
        return this;
    }

    public IDictionary<string, string> Build(IServiceResolver serviceResolver, TContext context)
    {
        return _list.Select(
                func => func(serviceResolver, context))
            .SelectMany(x => x)
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private void Add(string key, Func<IServiceResolver, TContext, string> func)
    {
        _list.Add((resolver, context) =>
            new Dictionary<string, string> { { key, func(resolver, context) } });
    }
}
