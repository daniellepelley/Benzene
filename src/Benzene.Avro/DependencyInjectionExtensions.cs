using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.MediaFormats;

namespace Benzene.Avro;

/// <summary>
/// Registration helpers for Avro support. Avro is schema-based, so <paramref name="configure"/> lets
/// you register explicit <c>.avsc</c> schemas per type and/or toggle reflection-inferred schemas via
/// <see cref="AvroOptions"/>; with no configuration, schemas are inferred by reflection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers <see cref="AvroMediaFormat{TContext}"/> as an <see cref="IMediaFormat{TContext}"/> for
    /// every context type (open generic), plus the shared <see cref="AvroSerializer"/>.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="configure">Optional configuration of schema resolution.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddAvro(this IBenzeneServiceContainer services, Action<AvroOptions>? configure = null)
    {
        services.TryAddSingleton(BuildSerializer(configure));
        services.AddSingleton(typeof(IMediaFormat<>), typeof(AvroMediaFormat<>));
        return services;
    }

    /// <summary>
    /// Registers <see cref="AvroMediaFormat{TContext}"/> as an <see cref="IMediaFormat{TContext}"/> for
    /// <typeparamref name="TContext"/> only, plus the shared <see cref="AvroSerializer"/>.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="configure">Optional configuration of schema resolution.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddAvro<TContext>(this IBenzeneServiceContainer services, Action<AvroOptions>? configure = null)
        where TContext : class
    {
        services.TryAddSingleton(BuildSerializer(configure));
        services.AddSingleton<IMediaFormat<TContext>, AvroMediaFormat<TContext>>();
        return services;
    }

    /// <summary>Registers Avro support for <typeparamref name="TContext"/> onto a middleware pipeline builder.</summary>
    /// <param name="source">The pipeline builder to register Avro support onto.</param>
    /// <param name="configure">Optional configuration of schema resolution.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseAvro<TContext>(this IMiddlewarePipelineBuilder<TContext> source,
        Action<AvroOptions>? configure = null)
        where TContext : class
    {
        source.Register(x => x.AddAvro<TContext>(configure));
        return source;
    }

    private static AvroSerializer BuildSerializer(Action<AvroOptions>? configure)
    {
        var options = new AvroOptions();
        configure?.Invoke(options);
        return new AvroSerializer(new AvroSchemaResolver(options));
    }
}
