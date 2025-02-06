using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.Logging;

public interface ILogContextBuilder<TContext> : IRegisterDependency
{
    ILogContextBuilder<TContext> OnRequest(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);
    ILogContextBuilder<TContext> OnResponse(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);
    IDisposable CreateForRequest(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver,
        TContext context);
    IDisposable CreateForResponse(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver,
        TContext context);
}
