using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Idempotency;

/// <summary>
/// Registration extensions for idempotency: a pipeline extension to add the middleware and DI
/// extensions to register a store.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds idempotency de-duplication to the pipeline. Requires an <see cref="IIdempotencyStore"/>
    /// to be registered (see <see cref="AddInMemoryIdempotencyStore"/> or register your own). Uses a
    /// custom <see cref="IIdempotencyKeyStrategy{TContext}"/> if one is registered for
    /// <typeparamref name="TContext"/>, otherwise the default header/body-hash strategy.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific message context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="configure">Optional configuration of <see cref="IdempotencyOptions"/>.</param>
    public static IMiddlewarePipelineBuilder<TContext> UseIdempotency<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        Action<IdempotencyOptions>? configure = null)
    {
        var options = new IdempotencyOptions();
        configure?.Invoke(options);

        return app.Use(resolver =>
        {
            var keyStrategy = resolver.TryGetService<IIdempotencyKeyStrategy<TContext>>()
                ?? new HeaderOrBodyHashIdempotencyKeyStrategy<TContext>(
                    resolver.GetService<IMessageHeadersGetter<TContext>>(),
                    resolver.GetService<IMessageBodyGetter<TContext>>(),
                    resolver.GetService<IMessageTopicGetter<TContext>>(),
                    options);

            return new IdempotencyMiddleware<TContext>(
                resolver.GetService<IIdempotencyStore>(),
                keyStrategy,
                options);
        });
    }

    /// <summary>
    /// Registers the <see cref="InMemoryIdempotencyStore"/> as the <see cref="IIdempotencyStore"/>.
    /// Suitable for a single worker instance, tests, and local development; use a shared store in a
    /// multi-instance deployment. Call once at application setup.
    /// </summary>
    /// <param name="services">The service container.</param>
    /// <param name="timeToLive">How long records are retained. Defaults to 24 hours.</param>
    public static IBenzeneServiceContainer AddInMemoryIdempotencyStore(
        this IBenzeneServiceContainer services,
        TimeSpan? timeToLive = null)
    {
        services.AddSingleton<IIdempotencyStore>(new InMemoryIdempotencyStore(timeToLive));
        return services;
    }
}
