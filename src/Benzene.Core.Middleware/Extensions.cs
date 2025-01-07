using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        IMiddleware<TContext> middleware)
    {
        return app.Use(_ => middleware);
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, Func<Task>, Task> func)
    {
        return app.Use(new FuncWrapperMiddleware<TContext>(func));
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<TContext, Func<Task>, Task> func)
    {
        return app.Use(new FuncWrapperMiddleware<TContext>(name, func));
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<IServiceResolver, Func<TContext, Func<Task>, Task>> func)
    {
        return app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>(func(serviceResolver)));
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<IServiceResolver, Func<TContext, Func<Task>, Task>> func)
    {
        return app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>(name, func(serviceResolver)));
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<IServiceResolver, TContext, Func<Task>, Task> func)
    {
        return app.Use(serviceResolver =>
            new FuncWrapperMiddleware<TContext>((context, next) => func(serviceResolver, context, next)));
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<IServiceResolver, TContext, Func<Task>, Task> func)
    {
        return app.Use(serviceResolver =>
            new FuncWrapperMiddleware<TContext>(name, (context, next) => func(serviceResolver, context, next)));
    }

    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        Action<TContext> action)
    {
        return app.Use(async (context, next) =>
        {
            action(context);
            await next();
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<TContext> action)
    {
        return app.Use(name, async (context, next) =>
        {
            action(context);
            await next();
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(async (resolver, context, next) =>
        {
            action(resolver, context);
            await next();
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnRequest<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(name, async (resolver, context, next) =>
        {
            action(resolver, context);
            await next();
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        Action<TContext> action)
    {
        return app.Use(async (context, next) =>
        {
            await next();
            action(context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<TContext> action)
    {
        return app.Use(name, async (context, next) =>
        {
            await next();
            action(context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(async (resolver, context, next) =>
        {
            await next();
            action(resolver, context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> OnResponse<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string name,
        Action<IServiceResolver, TContext> action)
    {
        return app.Use(name, async (resolver, context, next) =>
        {
            await next();
            action(resolver, context);
        });
    }

    public static IMiddlewarePipelineBuilder<TContext> Use<TContext, TMiddleware>(
        this IMiddlewarePipelineBuilder<TContext> app)
        where TMiddleware : class, IMiddleware<TContext>
    {
        return app.Use(resolver => resolver.GetService<TMiddleware>());
    }

    public static IMiddlewarePipelineBuilder<TContext> Split<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, bool> check, Action<IMiddlewarePipelineBuilder<TContext>> builder)
    {
        var newApp = app.Create<TContext>();
        builder(app);

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("Split", async (context, next) =>
        {
            if (check(context))
            {
                await newApp.Build().HandleAsync(context, resolver);
            }
            else
            {
                await next();
            }
        }));
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
       IContextConverter<TContext, TContextOut> converter, IMiddlewarePipeline<TContextOut> middlewarePipeline)
    {
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);

        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

}
