using System.Reflection;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers;

public static class Extensions
{
    public static bool Is<TContext>(this IMessageTopicGetter<TContext> source, TContext context, string topic, bool isCaseSensitive = false)
    {
        var contextTopic = source.GetTopic(context);
        if (contextTopic == null)
        {
            return false;
        }

        if (!isCaseSensitive)
        {
            return contextTopic.Id.ToLowerInvariant() == topic.ToLowerInvariant();
        }

        return contextTopic.Id == topic;
    }
}

public static class MiddlewarePipelineExtensions
{
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
        app.Register(x => x.AddMessageHandlers(assemblies));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Type[] types)
    {
        app.Register(x => x.AddMessageHandlers(types));
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
        app.Register(x => x.AddMessageHandlers(types));
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
