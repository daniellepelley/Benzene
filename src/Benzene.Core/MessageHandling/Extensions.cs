using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Core.Response;

namespace Benzene.Core.MessageHandling;

public static class Extensions
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
        app.Register(x => x.AddMessageHandlers2(types));
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
        return UseMessageHandlers(app, (Type[])Utils.GetAllTypes(assemblies).ToArray(), router);
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
