using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.Middleware;

namespace Benzene.MessagePack;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers <see cref="MessagePackMediaFormat{TContext}"/> as an
    /// <see cref="IMediaFormat{TContext}"/> for every context type (open generic).
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddMessagePack(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<MessagePackSerializer>();
        services.AddSingleton(typeof(IMediaFormat<>), typeof(MessagePackMediaFormat<>));
        return services;
    }

    /// <summary>
    /// Registers <see cref="MessagePackMediaFormat{TContext}"/> as an
    /// <see cref="IMediaFormat{TContext}"/> for <typeparamref name="TContext"/> only.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddMessagePack<TContext>(this IBenzeneServiceContainer services) where TContext : class
    {
        services.TryAddSingleton<MessagePackSerializer>();
        services.AddSingleton<IMediaFormat<TContext>, MessagePackMediaFormat<TContext>>();
        return services;
    }

    /// <summary>
    /// Registers MessagePack support for <typeparamref name="TContext"/> onto a middleware
    /// pipeline builder.
    /// </summary>
    /// <param name="source">The pipeline builder to register MessagePack support onto.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMessagePack<TContext>(this IMiddlewarePipelineBuilder<TContext> source)
        where TContext : class
    {
        source.Register(x => x.AddMessagePack<TContext>());
        return source;
    }
}
