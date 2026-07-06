using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;

namespace Benzene.SelfHost;

public interface IBenzeneWorkerStartup : IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IBenzeneWorker> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IBenzeneWorker Create(IServiceResolverFactory serviceResolverFactory);
}

// Benzene worker should simple be something that polls and pushes down a middleware pipeline.

// public interface IMiddlewarePipelineBuilder<TContext> : IRegisterDependency
// {
//     IMiddlewarePipelineBuilder<TContext> Use(Func<IServiceResolver, IMiddleware<TContext>> func);
//     IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
//     IMiddlewarePipeline<TContext> Build();
// }

