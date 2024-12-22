using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers;

public static class Extensions
{
    public static IBenzeneServiceContainer AddMessageHandlers2(this IBenzeneServiceContainer services,
        params Assembly[] assemblies)
    {
        var types = Utils.GetAllTypes(assemblies).ToArray();
        return services.AddMessageHandlers2(types);
    }

    public static IBenzeneServiceContainer AddMessageHandlers2(this IBenzeneServiceContainer services,
        Type[] types)
    {
        var cacheMessageHandlersFinder = new CacheMessageHandlersFinder(new ReflectionMessageHandlersFinder(types));
        foreach (var handler in cacheMessageHandlersFinder.FindDefinitions())
        {
            services.AddScoped(handler.HandlerType);
        }
    
        services.TryAddSingleton<MessageHandlersList>();
        services.TryAddSingleton<DependencyMessageHandlersFinder>();
        services.TryAddSingleton<IMessageHandlersList, MessageHandlersList>();
        services.TryAddSingleton<IMessageHandlersFinder>(x =>
            new CompositeMessageHandlersFinder(
                cacheMessageHandlersFinder,
            x.GetService<MessageHandlersList>(),
            x.GetService<DependencyMessageHandlersFinder>()
        ));
    
        services.TryAddScoped<IMessageHandlersLookUp, MessageHandlersLookUp>();
        services.TryAddScoped<IHandlerPipelineBuilder, HandlerPipelineBuilder>();
        services.TryAddScoped<IMessageHandlerWrapper, PipelineMessageHandlerWrapper>();
        services.TryAddScoped<IMessageHandlerFactory, MessageHandlerFactory>();
        services.TryAddScoped(typeof(MessageRouter<>));
    
        return services;
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers2<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
        where TContext : IHasMessageResult
    {
        return app.UseMessageHandlers2(AppDomain.CurrentDomain.GetAssemblies());
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers2<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        return app.UseMessageHandlers2(AppDomain.CurrentDomain.GetAssemblies(), router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers2<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Assembly[] assemblies)
        where TContext : IHasMessageResult
    {
        app.Register(x => AddMessageHandlers2(x, assemblies));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Type[] types)
        where TContext : IHasMessageResult
    {
        app.Register(x => AddMessageHandlers2(x, types));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly assembly, Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        return app.UseMessageHandlers2(new[] { assembly }, router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers2<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly[] assemblies, Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        return app.UseMessageHandlers2(Utils.GetAllTypes(assemblies).ToArray(), router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers2<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Type[] types, Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        app.Register(x => AddMessageHandlers2(x, types));
        var builder = new MessageRouterBuilder(new List<IHandlerMiddlewareBuilder>(), app.Register);
        router(builder);

        return app.Use(resolver =>
        {
            var routePipelineBuilder = resolver.GetService<IHandlerPipelineBuilder>();
            routePipelineBuilder.Add(builder.GetBuilders());
            return resolver.GetService<MessageRouter<TContext>>();
        });
    }
}
