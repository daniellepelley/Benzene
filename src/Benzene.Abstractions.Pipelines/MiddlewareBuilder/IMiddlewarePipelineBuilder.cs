using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MiddlewareBuilder;

public interface IMiddlewarePipelineBuilder<TContext> : IRegisterDependency
{
    IMiddlewarePipelineBuilder<TContext> Use(Func<IServiceResolver, IMiddleware<TContext>> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IMiddlewarePipeline<TContext> Build();
}
