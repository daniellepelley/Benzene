using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Mappers;
using Benzene.Core.Middleware;
using Benzene.Core.Request;
using Benzene.Core.Response;

namespace Benzene.Core.MessageHandlers;

public static class Extensions
{
    public static IBenzeneServiceContainer AddContextItems(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped(typeof(IMessageGetter<>), typeof(MessageGetter<>));
        services.TryAddScoped(typeof(IResponsePayloadMapper<>), typeof(DefaultResponsePayloadMapper<>));
        services.TryAddScoped(typeof(IResponseHandlerContainer<>), typeof(ResponseHandlerContainer<>));
        services.TryAddScoped(typeof(JsonSerializationResponseHandler<>));

        services.TryAddScoped(typeof(IRequestMapper<>), typeof(JsonDefaultMultiSerializerOptionsRequestMapper<>));
        return services;
    }


    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
        params Assembly[] assemblies)
    {
        var types = Utils.GetAllTypes(assemblies).ToArray();
        return services.AddMessageHandlers(types);
    }

    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
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
    
        services.TryAddScoped<IMessageHandlerDefinitionLookUp, MessageHandlerDefinitionLookUp>();
        services.TryAddScoped<IHandlerPipelineBuilder, HandlerPipelineBuilder>();
        services.TryAddScoped<IMessageHandlerWrapper, PipelineMessageHandlerWrapper>();
        services.TryAddScoped<IMessageHandlerFactory, MessageHandlerFactory>();
        services.TryAddScoped(typeof(MessageRouter<>));

        services.AddContextItems();
        return services;
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.UseMessageHandlers(AppDomain.CurrentDomain.GetAssemblies());
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<MessageRouterBuilder> router)
    {
        return app.UseMessageHandlers(AppDomain.CurrentDomain.GetAssemblies(), router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Assembly[] assemblies)
    {
        app.Register(x => AddMessageHandlers(x, assemblies));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Type[] types)
    {
        app.Register(x => AddMessageHandlers(x, types));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly assembly, Action<MessageRouterBuilder> router) 
    {
        return app.UseMessageHandlers(new[] { assembly }, router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly[] assemblies, Action<MessageRouterBuilder> router)
    {
        return app.UseMessageHandlers(Utils.GetAllTypes(assemblies).ToArray(), router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Type[] types, Action<MessageRouterBuilder> router) 
    {
        app.Register(x => AddMessageHandlers(x, types));
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
