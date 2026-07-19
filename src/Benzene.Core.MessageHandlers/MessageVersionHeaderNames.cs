using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// An application-wide override for the ordered header-name fallback
/// <see cref="HeaderMessageVersionGetter{TContext}"/> reads the payload schema version from. Register
/// it once via <see cref="MessageVersionHeaderNamesExtensions.AddMessageVersionHeaderNames"/>; every
/// transport's version getter (registered through
/// <see cref="MessageVersionHeaderNamesExtensions.AddHeaderMessageVersionGetter{TContext}"/>) then
/// resolves it, so the header names are configured in one place rather than per transport. When it
/// isn't registered, each getter falls back to <see cref="HeaderMessageVersionGetter{TContext}.DefaultHeaderNames"/>.
/// </summary>
/// <remarks>
/// The version header names are an application-wide contract (the same set regardless of transport),
/// unlike a transport's topic attribute/property key, which is genuinely per-transport - hence a
/// single DI-resolved override here instead of a parameter repeated on every <c>AddXxx</c>. An
/// application with a pre-existing, differently-meaning <c>version</c>/<c>x-version</c> header MUST
/// narrow or replace the list (docs/specification/versioning.md §2.1).
/// </remarks>
public class MessageVersionHeaderNames
{
    /// <summary>Initializes a new instance with the given ordered header-name fallback list.</summary>
    /// <param name="headerNames">The header names to try, in order; the first present in the header dictionary wins.</param>
    public MessageVersionHeaderNames(IReadOnlyList<string> headerNames)
    {
        HeaderNames = headerNames;
    }

    /// <summary>The ordered header-name fallback list.</summary>
    public IReadOnlyList<string> HeaderNames { get; }
}

/// <summary>
/// Registration helpers for the message-version header-name fallback and its application-wide override.
/// </summary>
public static class MessageVersionHeaderNamesExtensions
{
    /// <summary>
    /// Overrides, application-wide, the ordered header-name fallback every transport's
    /// <see cref="HeaderMessageVersionGetter{TContext}"/> reads the payload schema version from. Call
    /// this to narrow or replace <see cref="HeaderMessageVersionGetter{TContext}.DefaultHeaderNames"/>
    /// (<c>["benzene-version", "version", "x-version"]</c>) - e.g. when the application already uses a
    /// <c>version</c>/<c>x-version</c> header to mean something else. The value is resolved when a
    /// message is handled, so registration order relative to the transport <c>AddXxx</c> calls does
    /// not matter.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="headerNames">The header names to try, in order; the first present wins.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddMessageVersionHeaderNames(this IBenzeneServiceContainer services, params string[] headerNames)
    {
        services.AddSingleton(_ => new MessageVersionHeaderNames(headerNames));
        return services;
    }

    /// <summary>
    /// Registers <see cref="HeaderMessageVersionGetter{TContext}"/> as the
    /// <see cref="IMessageVersionGetter{TContext}"/>, reading the header names from an
    /// application-wide <see cref="MessageVersionHeaderNames"/> override when one is registered
    /// (see <see cref="AddMessageVersionHeaderNames"/>) and falling back to
    /// <see cref="HeaderMessageVersionGetter{TContext}.DefaultHeaderNames"/> otherwise. Transports
    /// call this instead of registering <see cref="HeaderMessageVersionGetter{TContext}"/> directly,
    /// so the override reaches every transport uniformly.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddHeaderMessageVersionGetter<TContext>(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageVersionGetter<TContext>>(resolver =>
            new HeaderMessageVersionGetter<TContext>(
                resolver.GetService<IMessageHeadersGetter<TContext>>(),
                resolver.TryGetService<MessageVersionHeaderNames>()?.HeaderNames));
        return services;
    }

    /// <summary>
    /// The <see cref="TryAddScoped{T}(IBenzeneServiceContainer, System.Func{IServiceResolver, T})"/>
    /// counterpart of <see cref="AddHeaderMessageVersionGetter{TContext}"/>, for callers that register
    /// the version getter conditionally.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer TryAddHeaderMessageVersionGetter<TContext>(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<IMessageVersionGetter<TContext>>(resolver =>
            new HeaderMessageVersionGetter<TContext>(
                resolver.GetService<IMessageHeadersGetter<TContext>>(),
                resolver.TryGetService<MessageVersionHeaderNames>()?.HeaderNames));
        return services;
    }
}
