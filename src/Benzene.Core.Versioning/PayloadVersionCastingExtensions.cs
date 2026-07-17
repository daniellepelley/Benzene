using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.Versioning.Request;
using Benzene.Core.Versioning.Response;
using Benzene.Core.Versioning.Schemas;

namespace Benzene.Core.Versioning;

/// <summary>
/// DI wiring for transparent payload-version casting (docs/specification/versioning.md §4, mechanism B).
/// </summary>
public static class PayloadVersionCastingExtensions
{
    /// <summary>
    /// Wraps the request and response payload mappers for <typeparamref name="TContext"/> with the
    /// version-casting decorators, so an incoming older-version payload is upcast into the handler's
    /// declared request type and the response is downcast back to the requested version - the handler
    /// only ever sees its own (canonical/latest) schema. A topic with no registered casters, or a
    /// message that signals no version, is unaffected: the decorators delegate straight to the mappers
    /// they wrap.
    /// </summary>
    /// <remarks>
    /// Call this after the transport's own registration (e.g. <c>UseHttp</c>/<c>AddSqs</c> +
    /// <c>AddMessageHandlers</c>), so the closed <see cref="IRequestMapper{TContext}"/>/
    /// <see cref="IResponsePayloadMapper{TContext}"/> registrations here take precedence, and pair it
    /// with <c>RegisterSchemaCastDefinitions</c>/<c>RegisterPayloadSchemaVersions</c> to supply the
    /// casters. It wraps the framework-default mappers (<see cref="MultiSerializerOptionsRequestMapper{TContext}"/>
    /// / <see cref="DefaultResponsePayloadMapper{TContext}"/>); a transport that registers a bespoke
    /// request mapper (e.g. gRPC's protobuf-bridging one) is not wrapped on the request side.
    /// </remarks>
    /// <typeparam name="TContext">The transport-specific context type to enable casting for.</typeparam>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer UsePayloadVersionCasting<TContext>(this IBenzeneServiceContainer services)
        where TContext : class
    {
        // Self-register the framework-default mappers as their own concrete types so the decorators
        // below can resolve and wrap them (they're otherwise only registered under their interfaces).
        services.TryAddScoped<MultiSerializerOptionsRequestMapper<TContext>>();
        services.TryAddScoped<DefaultResponsePayloadMapper<TContext>>();

        services.AddScoped<IRequestMapper<TContext>>(resolver =>
            new CastingRequestMapper<TContext>(
                resolver.GetService<MultiSerializerOptionsRequestMapper<TContext>>(),
                resolver.GetService<IMessageVersionGetter<TContext>>(),
                resolver.GetService<IMessageTopicGetter<TContext>>(),
                resolver.TryGetService<ISchemaCasters>()));

        services.AddScoped<IResponsePayloadMapper<TContext>>(resolver =>
            new CastingResponsePayloadMapper<TContext>(
                resolver.GetService<DefaultResponsePayloadMapper<TContext>>(),
                resolver.GetService<IMessageVersionGetter<TContext>>(),
                resolver.GetService<IMessageTopicGetter<TContext>>(),
                resolver.TryGetService<ISchemaCasters>()));

        return services;
    }
}
