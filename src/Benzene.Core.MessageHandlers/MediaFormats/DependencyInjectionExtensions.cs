using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;

namespace Benzene.Core.MessageHandlers.MediaFormats;

/// <summary>
/// Provides the shared registration for content negotiation, called by every transport that maps
/// requests and/or writes responses (the four HTTP-ish adapters directly, and <c>BenzeneMessage</c>
/// and the generic message-handler path via <c>AddContextItems</c>) so an <see cref="IMediaFormatNegotiator{TContext}"/>
/// is always available wherever an <see cref="IMediaFormat{TContext}"/> (e.g. <c>Benzene.Xml</c>'s
/// <c>XmlMediaFormat</c>) might be registered for that context type.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the default JSON media format and the scoped negotiator that selects between it and
    /// every other <see cref="IMediaFormat{TContext}"/> registered for <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type to register negotiation for.</typeparam>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddMediaFormatNegotiation<TContext>(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonMediaFormat<TContext>>();
        services.TryAddScoped<IMediaFormatNegotiator<TContext>, MediaFormatNegotiator<TContext>>();
        return services;
    }
}
