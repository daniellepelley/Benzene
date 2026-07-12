using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides extension methods for building and configuring middleware pipelines.
/// </summary>
/// <remarks>
/// This class contains the primary fluent API for pipeline construction, including methods for
/// adding middleware, handling requests and responses, splitting pipelines, and converting contexts.
/// </remarks>
public static class Extensions
{
    /// <summary>
    /// Adds a middleware instance to the pipeline.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="middleware">The middleware instance to add.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        IMiddleware<TContext> middleware)
    {
        return app.Use(_ => middleware);
    }

    /// <summary>
    /// Adds inline middleware to the pipeline using a function.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="func">The function that defines the middleware behavior.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, Func<Task>, Task> func)
    {
        return app.Use(new FuncWrapperMiddleware<TContext>(func));
    }

    /// <summary>
    /// Adds named inline middleware to the pipeline using a function.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="name">The name for this middleware component.</param>
    /// <param name="func">The function that defines the middleware behavior.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<TContext, Func<Task>, Task> func)
    {
        return app.Use(new FuncWrapperMiddleware<TContext>(name, func));
    }

    /// <summary>
    /// Adds inline middleware to the pipeline using a function that receives a service resolver.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="func">The function that receives a service resolver and returns the middleware behavior.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<IServiceResolver, Func<TContext, Func<Task>, Task>> func)
    {
        return app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>(func(serviceResolver)));
    }

    /// <summary>
    /// Adds named inline middleware to the pipeline using a function that receives a service resolver.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="name">The name for this middleware component.</param>
    /// <param name="func">The function that receives a service resolver and returns the middleware behavior.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<IServiceResolver, Func<TContext, Func<Task>, Task>> func)
    {
        return app.Use(serviceResolver => new FuncWrapperMiddleware<TContext>(name, func(serviceResolver)));
    }

    /// <summary>
    /// Adds inline middleware to the pipeline using a function that receives both service resolver and context.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="func">The function that defines the middleware behavior with access to the service resolver.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<IServiceResolver, TContext, Func<Task>, Task> func)
    {
        return app.Use(serviceResolver =>
            new FuncWrapperMiddleware<TContext>((context, next) => func(serviceResolver, context, next)));
    }

    /// <summary>
    /// Adds named inline middleware to the pipeline using a function that receives both service resolver and context.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="name">The name for this middleware component.</param>
    /// <param name="func">The function that defines the middleware behavior with access to the service resolver.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string name, Func<IServiceResolver, TContext, Func<Task>, Task> func)
    {
        return app.Use(serviceResolver =>
            new FuncWrapperMiddleware<TContext>(name, (context, next) => func(serviceResolver, context, next)));
    }

    /// <summary>
    /// Executes an action on the request before continuing to the next middleware.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="action">The action to execute on the request context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context before
    /// downstream middleware executes.
    /// </remarks>
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

    /// <summary>
    /// Executes a named action on the request before continuing to the next middleware.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="name">The name for this middleware component.</param>
    /// <param name="action">The action to execute on the request context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context before
    /// downstream middleware executes.
    /// </remarks>
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

    /// <summary>
    /// Executes an action with service resolver access on the request before continuing to the next middleware.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="action">The action to execute with access to the service resolver and request context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context before
    /// downstream middleware executes, with access to the service resolver for dependency resolution.
    /// </remarks>
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

    /// <summary>
    /// Executes a named action with service resolver access on the request before continuing to the next middleware.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="name">The name for this middleware component.</param>
    /// <param name="action">The action to execute with access to the service resolver and request context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context before
    /// downstream middleware executes, with access to the service resolver for dependency resolution.
    /// </remarks>
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

    /// <summary>
    /// Executes an action on the response after downstream middleware completes.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="action">The action to execute on the response context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context after
    /// downstream middleware executes. Useful for logging, metrics, or response transformation.
    /// </remarks>
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

    /// <summary>
    /// Executes a named action on the response after downstream middleware completes.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="name">The name for this middleware component.</param>
    /// <param name="action">The action to execute on the response context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context after
    /// downstream middleware executes. Useful for logging, metrics, or response transformation.
    /// </remarks>
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

    /// <summary>
    /// Executes an action with service resolver access on the response after downstream middleware completes.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="action">The action to execute with access to the service resolver and response context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context after
    /// downstream middleware executes, with access to the service resolver for dependency resolution.
    /// </remarks>
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

    /// <summary>
    /// Executes a named action with service resolver access on the response after downstream middleware completes.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <param name="name">The name for this middleware component.</param>
    /// <param name="action">The action to execute with access to the service resolver and response context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a tap point in the pipeline for inspecting or modifying the context after
    /// downstream middleware executes, with access to the service resolver for dependency resolution.
    /// </remarks>
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

    /// <summary>
    /// Adds middleware to the pipeline by resolving it from the service container.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <typeparam name="TMiddleware">The middleware type to resolve and add.</typeparam>
    /// <param name="app">The pipeline builder to add middleware to.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> Use<TContext, TMiddleware>(
        this IMiddlewarePipelineBuilder<TContext> app)
        where TMiddleware : class, IMiddleware<TContext>
    {
        return app.Use(resolver => resolver.GetService<TMiddleware>());
    }

    /// <summary>
    /// Conditionally branches the pipeline execution based on a predicate.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add the split to.</param>
    /// <param name="check">The predicate that determines whether to execute the branch pipeline.</param>
    /// <param name="builder">The action that configures the branch pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// If the predicate returns true, the branch pipeline executes; otherwise, execution continues
    /// to the next middleware in the main pipeline. This enables conditional routing within a pipeline.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> Split<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, bool> check, Action<IMiddlewarePipelineBuilder<TContext>> builder)
    {
        var newApp = app.Create<TContext>();
        builder(newApp);

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

    /// <summary>
    /// Conditionally branches the pipeline execution based on a context predicate.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add the split to.</param>
    /// <param name="predicate">The context predicate that determines whether to execute the branch pipeline.</param>
    /// <param name="builder">The action that configures the branch pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// If the predicate returns true, the branch pipeline executes; otherwise, execution continues
    /// to the next middleware in the main pipeline. The predicate has access to the service resolver
    /// for dependency-based routing decisions.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> Split<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextPredicate<TContext> predicate, Action<IMiddlewarePipelineBuilder<TContext>> builder)
    {
        var newApp = app.Create<TContext>();
        builder(newApp);

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("Split", async (context, next) =>
        {
            if (predicate.Check(context, resolver))
            {
                await newApp.Build().HandleAsync(context, resolver);
            }
            else
            {
                await next();
            }
        }));
    }

    /// <summary>
    /// Converts the context to a different type for processing by a separate pipeline.
    /// </summary>
    /// <typeparam name="TContext">The input context type to convert from.</typeparam>
    /// <typeparam name="TContextOut">The output context type to convert to.</typeparam>
    /// <param name="app">The pipeline builder to add the converter to.</param>
    /// <param name="converter">The context converter that handles transformation and response mapping.</param>
    /// <param name="middlewarePipeline">The pipeline that processes the converted context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This enables different pipeline stages to operate on different context types, with automatic
    /// conversion and response mapping between them.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
       IContextConverter<TContext, TContextOut> converter, IMiddlewarePipeline<TContextOut> middlewarePipeline)
    {
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    /// <summary>
    /// Converts the context to a different type for processing by a pipeline configured inline.
    /// </summary>
    /// <typeparam name="TContext">The input context type to convert from.</typeparam>
    /// <typeparam name="TContextOut">The output context type to convert to.</typeparam>
    /// <param name="app">The pipeline builder to add the converter to.</param>
    /// <param name="converter">The context converter that handles transformation and response mapping.</param>
    /// <param name="action">The action that configures the pipeline for the converted context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This enables different pipeline stages to operate on different context types, with automatic
    /// conversion and response mapping between them.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);

        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    /// <summary>
    /// Converts the context using inline functions for transformation and response mapping.
    /// </summary>
    /// <typeparam name="TContext">The input context type to convert from.</typeparam>
    /// <typeparam name="TContextOut">The output context type to convert to.</typeparam>
    /// <param name="app">The pipeline builder to add the converter to.</param>
    /// <param name="createContextFunc">The function that creates the output context from the input context.</param>
    /// <param name="mapContext">The action that maps the response back to the input context.</param>
    /// <param name="middlewarePipeline">The pipeline that processes the converted context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a lightweight way to convert contexts without creating a separate converter class.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, TContextOut> createContextFunc, Action<TContext, TContextOut> mapContext, IMiddlewarePipeline<TContextOut> middlewarePipeline)
    {
        var converter = new InlineContextConverter<TContext, TContextOut>(createContextFunc, mapContext);

        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    /// <summary>
    /// Converts the context using inline functions and configures the processing pipeline inline.
    /// </summary>
    /// <typeparam name="TContext">The input context type to convert from.</typeparam>
    /// <typeparam name="TContextOut">The output context type to convert to.</typeparam>
    /// <param name="app">The pipeline builder to add the converter to.</param>
    /// <param name="createContextFunc">The function that creates the output context from the input context.</param>
    /// <param name="mapContext">The action that maps the response back to the input context.</param>
    /// <param name="action">The action that configures the pipeline for the converted context.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides a lightweight way to convert contexts and configure their processing pipeline
    /// without creating separate converter and pipeline classes.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, TContextOut> createContextFunc, Action<TContext, TContextOut> mapContext, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);

        var converter = new InlineContextConverter<TContext, TContextOut>(createContextFunc, mapContext);

        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    /// <summary>
    /// Adds exception handling middleware to the pipeline.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add the exception handler to.</param>
    /// <param name="onException">The action to execute when an exception is caught.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This provides centralized exception handling for the pipeline, allowing exceptions to be
    /// logged, transformed into error responses, or handled in a context-aware manner.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseExceptionHandler<TContext>(this IMiddlewarePipelineBuilder<TContext> app, Action<TContext, Exception> onException)
    {
        return app.Use(resolver => new ExceptionHandlerMiddleware<TContext>(onException,
            resolver.TryGetService<ILoggerFactory>()?.CreateLogger("Benzene") ?? NullLogger.Instance));
    }
}
