using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.MediaFormats;

namespace Benzene.Xml;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers <see cref="XmlMediaFormat{TContext}"/> as an <see cref="IMediaFormat{TContext}"/> for
    /// every context type (open generic).
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddXml(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<XmlSerializer>();
        services.AddSingleton(typeof(IMediaFormat<>), typeof(XmlMediaFormat<>));
        return services;
    }

    /// <summary>
    /// Registers <see cref="XmlMediaFormat{TContext}"/> as an <see cref="IMediaFormat{TContext}"/> for
    /// <typeparamref name="TContext"/> only.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddXml<TContext>(this IBenzeneServiceContainer services) where TContext : class
    {
        services.TryAddSingleton<XmlSerializer>();
        services.AddSingleton<IMediaFormat<TContext>, XmlMediaFormat<TContext>>();
        return services;
    }

    /// <summary>
    /// Registers XML support for <typeparamref name="TContext"/> onto a middleware pipeline builder.
    /// </summary>
    /// <param name="source">The pipeline builder to register XML support onto.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseXml<TContext>(this IMiddlewarePipelineBuilder<TContext> source)
        where TContext : class
    {
        source.Register(x => x.AddXml<TContext>());
        return source;
    }
}
