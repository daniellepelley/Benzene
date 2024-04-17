using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Abstractions.Results;
using Benzene.Core.DI;
using Benzene.Core.Helper;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.Core.Response;

namespace Benzene.Core.MiddlewareBuilder;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> source, Func<IServiceResolver, IMiddleware<TContext>> func)
    {
        source.Add(func);
        return source;
    }
    
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        IMiddleware<TContext> middleware)
    {
        app.Add(_ => middleware);
        return app;
    }
    
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, Func<Task>, Task> func)
    {
        app.Use(new FuncWrapperMiddleware<TContext>(func));
        return app;
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<TContext, Func<Task>, Task> func)
    {
        app.Use(new FuncWrapperMiddleware<TContext>(name, func));
        return app;
    }
    
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<IServiceResolver, Func<TContext, Func<Task>, Task>> func)
    {
        app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>(func(serviceResolver)));
        return app;
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<IServiceResolver, Func<TContext, Func<Task>, Task>> func)
    {
        app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>(name, func(serviceResolver)));
        return app;
    }
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<IServiceResolver, TContext, Func<Task>, Task> func)
    {
        app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>((context, next) => func(serviceResolver, context, next)));
        return app;
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<IServiceResolver, TContext, Func<Task>, Task> func)
    {
        app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>(name, (context, next) => func(serviceResolver, context, next)));
        return app;
    }

    // public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
    //     string name, Action<TContext> action)
    // {
    //     return app.Use(name, (context, _) =>
    //     {
    //         action(context);
    //         return Task.CompletedTask;
    //     });
    // }
    
    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<TContext> action)
    {
        return app.Use(async (context, next) =>
        {
            action(context);
            await next();
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<TContext> action)
    {
        return app.Use(name, async (context, next) =>
        {
            action(context);
            await next();
        });
    }
    
    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(async (resolver, context, next) =>
        {
            action(resolver, context);
            await next();
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(name, async (resolver, context, next) =>
        {
            action(resolver, context);
            await next();
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<TContext> action)
    {
        return app.Use(async (context, next) =>
        {
            await next();
            action(context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<TContext> action)
    {
        return app.Use(name, async (context, next) =>
        {
            await next();
            action(context);
        });
    }
    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(async (resolver, context, next) =>
        {
            await next();
            action(resolver, context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(name, async (resolver, context, next) =>
        {
            await next();
            action(resolver, context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext, TMiddleware>(this IMiddlewarePipelineBuilder<TContext> app)
        where TMiddleware : class, IMiddleware<TContext>
    {
        return app.Use(resolver => resolver.GetService<TMiddleware>());
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageRouter<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
        where TContext : IHasMessageResult
    {
        return app.UseMessageRouter(AppDomain.CurrentDomain.GetAssemblies());
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageRouter<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        return app.UseMessageRouter(AppDomain.CurrentDomain.GetAssemblies(), router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageRouter<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Assembly[] assemblies)
        where TContext : IHasMessageResult
    {
        app.Register(x => x.AddMessageHandlers(assemblies));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageRouter<TContext>(this IMiddlewarePipelineBuilder<TContext> app, params Type[] types)
        where TContext : IHasMessageResult
    {
        app.Register(x => x.AddMessageHandlers(types));
        return app.Use<TContext, MessageRouter<TContext>>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageRouter<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly assembly, Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        return app.UseMessageRouter(new[] { assembly }, router);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseMessageRouter<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Assembly[] assemblies, Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
    {
        return app.UseMessageRouter(Utils.GetAllTypes(assemblies).ToArray(), router);
    }
    
    public static IMiddlewarePipelineBuilder<TContext> UseMessageRouter<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Type[] types, Action<MessageRouterBuilder> router) where TContext : IHasMessageResult
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

    public static IMiddlewarePipelineBuilder<TContext> Split<TContext>(this IMiddlewarePipelineBuilder<TContext> source,
        Func<TContext, bool> check, Action<IMiddlewarePipelineBuilder<TContext>> builder)
        where TContext : IHasMessageResult
    {
        var app = source.Create<TContext>();
        builder(app);

        return source.Use(resolver => new FuncWrapperMiddleware<TContext>("Split", async (context, next) =>
        {
            if (check(context))
            {
                await app.AsPipeline().HandleAsync(context, resolver);
            }
            else
            {
                await next();
            }
        }));
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

            var handlerContainer = new ResponseHandlerContainer<TContext>(responseHandlers);
            await handlerContainer.HandleAsync(context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> UseProcessResponseIfHandled<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
        where TContext : class, IHasMessageResult
    {
        return app.Use<TContext, ResponseIfHandledMiddleware<TContext>>();
    }

}
