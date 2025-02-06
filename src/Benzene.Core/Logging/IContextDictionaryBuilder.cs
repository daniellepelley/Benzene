using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;

namespace Benzene.Core.Logging;

public interface IContextDictionaryBuilder<TContext>
{
    IContextDictionaryBuilder<TContext> With(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);

    IDictionary<string, string> Build(IServiceResolver serviceResolver, TContext context);
}