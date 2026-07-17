using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Http.BenzeneMessage;

/// <summary>
/// Provides extension methods for adding a BenzeneMessage-over-HTTP endpoint to any Benzene HTTP
/// pipeline — the HTTP equivalent of the direct AWS Lambda invoke path, with the same
/// <c>UseBenzeneMessage</c> name and overload shapes.
/// </summary>
/// <remarks>
/// The endpoint exposes every topic the inner pipeline routes — including topics with no HTTP
/// mapping — so it is opt-in only: intended for local development or protected/admin environments.
/// Restrict topics via <see cref="BenzeneMessageHttpOptions.TopicFilter"/> and place
/// authentication middleware in front of it; do not expose it unauthenticated in production.
/// Registering it also registers an <see cref="IBenzeneMessageHttpEndpointInfo"/>, which the
/// <c>benzene</c> spec builder uses to advertise the endpoint as the top-level
/// <c>messageEndpoint</c> field.
/// </remarks>
public static class Extensions
{
    /// <summary>
    /// Adds a BenzeneMessage endpoint at <see cref="BenzeneMessageHttpOptions.DefaultPath"/>,
    /// configuring the inner BenzeneMessage pipeline inline.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The HTTP pipeline builder to add the endpoint to.</param>
    /// <param name="action">The action that configures the inner BenzeneMessage pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
        where TContext : IHttpContext
    {
        return app.UseBenzeneMessage(new BenzeneMessageHttpOptions(), action);
    }

    /// <summary>
    /// Adds a BenzeneMessage endpoint with the given options, configuring the inner BenzeneMessage
    /// pipeline inline.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The HTTP pipeline builder to add the endpoint to.</param>
    /// <param name="options">The endpoint options (path and optional topic filter).</param>
    /// <param name="action">The action that configures the inner BenzeneMessage pipeline.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        BenzeneMessageHttpOptions options,
        Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
        where TContext : IHttpContext
    {
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.UseBenzeneMessage(options, pipeline);
    }

    /// <summary>
    /// Adds a BenzeneMessage endpoint at <see cref="BenzeneMessageHttpOptions.DefaultPath"/> using
    /// an already-configured inner pipeline builder.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The HTTP pipeline builder to add the endpoint to.</param>
    /// <param name="builder">The pre-configured BenzeneMessage pipeline builder to build and use.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// Use this overload when the same BenzeneMessage pipeline is shared across multiple adapters
    /// (for example the direct Lambda invoke path and this HTTP endpoint), so it's built once and
    /// reused rather than reconfigured per adapter.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        IMiddlewarePipelineBuilder<BenzeneMessageContext> builder)
        where TContext : IHttpContext
    {
        return app.UseBenzeneMessage(new BenzeneMessageHttpOptions(), builder);
    }

    /// <summary>
    /// Adds a BenzeneMessage endpoint with the given options using an already-configured inner
    /// pipeline builder.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The HTTP pipeline builder to add the endpoint to.</param>
    /// <param name="options">The endpoint options (path and optional topic filter).</param>
    /// <param name="builder">The pre-configured BenzeneMessage pipeline builder to build and use.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        BenzeneMessageHttpOptions options,
        IMiddlewarePipelineBuilder<BenzeneMessageContext> builder)
        where TContext : IHttpContext
    {
        return app.UseBenzeneMessage(options, builder.Build());
    }

    private static IMiddlewarePipelineBuilder<TContext> UseBenzeneMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        BenzeneMessageHttpOptions options,
        IMiddlewarePipeline<BenzeneMessageContext> pipeline)
        where TContext : IHttpContext
    {
        app.Register(x =>
        {
            x.AddBenzeneMessage();
            x.TryAddScoped<IHttpStatusCodeMapper, DefaultHttpStatusCodeMapper>();
            x.AddSingleton<IBenzeneMessageHttpEndpointInfo>(_ => new BenzeneMessageHttpEndpointInfo(options.Path));
        });

        return app.Use(resolver => new BenzeneMessageHttpMiddleware<TContext>(
            options,
            pipeline,
            resolver,
            resolver.GetService<IHttpRequestAdapter<TContext>>(),
            resolver.GetService<IMessageBodyGetter<TContext>>(),
            resolver.GetService<IBenzeneResponseAdapter<TContext>>(),
            resolver.GetService<IHttpStatusCodeMapper>()));
    }
}
