using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public static class DependencyExtensions
{
    public static IBenzeneServiceContainer AddBenzeneMiddleware(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<IMiddlewareFactory, DefaultMiddlewareFactory>();
        services.AddServiceResolver();
        return services;
    }

    public static IMiddlewarePipeline<TContext> CreateMiddlewarePipeline<TContext>(this IRegisterDependency source,
        Action<IMiddlewarePipelineBuilder<TContext>> action)
    {
        var middlewareBuilder = new MiddlewarePipelineBuilder<TContext>(source);
        action(middlewareBuilder);
        return middlewareBuilder.Build();
    }
}
