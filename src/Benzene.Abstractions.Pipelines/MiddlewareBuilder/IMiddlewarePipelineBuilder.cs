using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MiddlewareBuilder;

public interface IMiddlewarePipelineBuilder<TContext> : IRegisterDependency
{
    void Add(Func<IServiceResolver, IMiddleware<TContext>> func);
    Func<IServiceResolver, IMiddleware<TContext>>[] GetItems();
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
}
