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
        Action<MessageRouterBuilder> router) //where TContext : IHasMessageResult
    {
        return app.UseMessageHandlers(AppDomain.CurrentDomain.GetAssemblies(), router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Assembly[] assemblies)
    {
        app.Register(x => x.AddMessageHandlers(assemblies));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Type[] types)
        where TContext : IHasMessageResult
    {
        app.Register(x => x.AddMessageHandlers2(types));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly assembly, Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        return app.UseMessageHandlers(new[] { assembly }, router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly[] assemblies, Action<MessageRouterBuilder> router) //where TContext : IHasMessageResult
    {
        return UseMessageHandlers(app, (Type[])Utils.GetAllTypes(assemblies).ToArray(), router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Type[] types, Action<MessageRouterBuilder> router) //where TContext : IHasMessageResult
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

    public static IMiddlewarePipelineBuilder<TContext> UseProcessResponse<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
        where TContext : class, IHasMessageResult
    {
        return app.Use<TContext, ResponseMiddleware<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseProcessResponse<TContext>(this IMiddlewarePipelineBuilder<TContext> app, Action<IResponseBuilder<TContext>> action)
        where TContext : class, IHasMessageResult
    {
        var responseBuilder = new ResponseBuilder<TContext>(app);
        action(responseBuilder);
        var builders = responseBuilder.GetBuilders();

        return app.Use("Response", async (resolver, context, next) =>
        {
            await next();

            var responseHandlers = builders.Select(x => x(resolver)).ToArray();

            var handlerContainer = new ResponseHandlerContainer<TContext>(resolver.GetService<IBenzeneResponseAdapter<TContext>>(), responseHandlers);
            await handlerContainer.HandleAsync(context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> UseProcessResponseIfHandled<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
        where TContext : class, IHasMessageResult
    {
        return app.Use<TContext, ResponseIfHandledMiddleware<TContext>>();
    }
}
