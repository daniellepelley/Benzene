using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;

namespace Benzene.Core.Logging;

public class ContextDictionaryBuilder<TContext> : IContextDictionaryBuilder<TContext>
{
    private readonly List<Func<IServiceResolver, TContext, IDictionary<string, string>>> _list = new();

    public IContextDictionaryBuilder<TContext> With(Func<IServiceResolver, TContext,  IDictionary<string, string>> dictionaryAction)
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
}
