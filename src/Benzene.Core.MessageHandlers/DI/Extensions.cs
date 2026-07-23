using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers.DI;

/// <summary>
/// Top-level DI registration extension methods for wiring up message-handler infrastructure and the
/// <c>BenzeneMessage</c> transport-agnostic message format.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers the services needed to handle messages in the <c>BenzeneMessage</c> format: message
    /// extraction, result setting, response adaptation, and a default "direct" <see cref="ITransportInfo"/>.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddBenzeneMessage(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<IMessageGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<IMessageBodyGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<IMessageTopicGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddHeaderMessageVersionGetter<BenzeneMessageContext>();
        services.TryAddScoped<IMessageHeadersGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<IMessageBodyBytesGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<BenzeneMessageResponseSuppression>();
        services.TryAddScoped<IMessageHandlerResultSetter<BenzeneMessageContext>, BenzeneMessageHandlerResultSetter>();
        services.TryAddScoped<IBenzeneResponseAdapter<BenzeneMessageContext>, BenzeneMessageResponseAdapter>();

        services.AddMediaFormatNegotiation<BenzeneMessageContext>();
        services.TryAddScoped<IResponseRenderer<BenzeneMessageContext>, SerializerResponseRenderer<BenzeneMessageContext>>();
        services.TryAddScoped<IResponseHandler<BenzeneMessageContext>, RendererResponseHandler<BenzeneMessageContext>>();
        services.AddScoped<IResponseHandler<BenzeneMessageContext>, DefaultResponseStatusHandler<BenzeneMessageContext>>();
        services.TryAddScoped<IResponsePayloadMapper<BenzeneMessageContext>, DefaultResponsePayloadMapper<BenzeneMessageContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Benzene));

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IApplicationInfo"/> with the given metadata.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="name">The application's name.</param>
    /// <param name="version">The application's version.</param>
    /// <param name="description">The application's description.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer SetApplicationInfo(this IBenzeneServiceContainer services,
        string name, string version, string description)
    {
        services.AddSingleton<IApplicationInfo>(_ => new ApplicationInfo(name, version, description));
        return services;
    }

    /// <summary>
    /// Registers the baseline services every Benzene application needs regardless of transport:
    /// default statuses, transport info tracking, a blank <see cref="IApplicationInfo"/> (if none is
    /// set), version selection, the default JSON <see cref="ISerializer"/>, the service resolver, and
    /// core middleware.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddBenzene(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<IDefaultStatuses, DefaultStatuses>();
        services.TryAddSingleton<ITransportsInfo, TransportsInfo>();

        services.TryAddScoped<CurrentTransportInfo>();
        services.TryAddScoped<ICurrentTransport>(x => x.GetService<CurrentTransportInfo>());
        services.TryAddScoped<ISetCurrentTransport>(x => x.GetService<CurrentTransportInfo>());

        // Scope-level ambient cancellation token any component can resolve (a transport seeds it per
        // request/message). Registered here so it's universally available, not only where health checks
        // are wired.
        services.TryAddScoped<Benzene.Core.CancellationTokenAccessor>();
        services.TryAddScoped<ICancellationTokenAccessor>(x => x.GetService<Benzene.Core.CancellationTokenAccessor>());

        services.TryAddSingleton<IApplicationInfo, BlankApplicationInfo>();
        services.TryAddSingleton<IVersionSelector, VersionSelector>();
        services.TryAddSingleton<ISerializer, JsonSerializer>();
        services.TryAddSingleton<JsonSerializer>();
        services.AddServiceResolver();
        services.AddBenzeneMiddleware();
        return services;
    }

    /// <summary>
    /// Registers the per-context request/response mapping services (message getter, response payload
    /// mapper, response handler container, media-format negotiation, and the negotiator-driven request
    /// mapper) as open generics, so they apply to any <c>TContext</c>.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddContextItems(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped(typeof(ResolvedTopicCache<>));
        services.TryAddScoped(typeof(IMessageGetter<>), typeof(MessageGetter<>));
        services.TryAddScoped(typeof(IResponsePayloadMapper<>), typeof(DefaultResponsePayloadMapper<>));
        services.TryAddScoped(typeof(IResponseHandlerContainer<>), typeof(ResponseHandlerContainer<>));
        services.TryAddScoped(typeof(JsonMediaFormat<>));
        services.TryAddScoped(typeof(IMediaFormatNegotiator<>), typeof(MediaFormatNegotiator<>));

        services.TryAddScoped(typeof(IRequestMapper<>), typeof(MultiSerializerOptionsRequestMapper<>));
        return services;
    }


    /// <summary>
    /// Registers message-handler dispatch infrastructure, discovering handlers by reflection over the
    /// given assemblies.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="assemblies">The assemblies to scan for handler types.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
        params Assembly[] assemblies)
    {
        var types = Utils.GetAllTypes(assemblies).ToArray();
        return services.AddMessageHandlers(types);
    }

    /// <summary>
    /// Registers message-handler dispatch infrastructure without registering any reflection-based
    /// handler discovery - only handlers registered explicitly (e.g. via the <c>AddMessageHandler</c>
    /// extension methods on <see cref="MiddlewarePipelineExtensions"/>, or directly in DI as
    /// <see cref="IMessageHandlerDefinition"/>) will be found.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<MessageHandlersList>();
        services.TryAddSingleton<DependencyMessageHandlersFinder>();
        services.TryAddSingleton<IMessageHandlersList>(x => x.GetService<MessageHandlersList>());
        services.TryAddSingleton<IMessageHandlersFinder>(x =>
            new CompositeMessageHandlersFinder(
                x.GetService<MessageHandlersList>(),
                x.GetService<DependencyMessageHandlersFinder>()
            ));
        services.TryAddSingleton<MessageHandlerDefinitionIndex>();

        services.TryAddScoped<IMessageHandlerDefinitionLookUp, MessageHandlerDefinitionLookUp>();
        services.TryAddScoped<IHandlerPipelineBuilder, HandlerPipelineBuilder>();
        services.TryAddScoped<IMessageHandlerWrapper, PipelineMessageHandlerWrapper>();
        services.TryAddScoped<IMessageHandlerFactory, MessageHandlerFactory>();
        services.TryAddScoped(typeof(MessageRouter<>));

        services.AddContextItems();
        return services;
    }

    /// <summary>
    /// Registers message-handler dispatch infrastructure, discovering handlers only among the given
    /// candidate types (via a cached <see cref="ReflectionMessageHandlersFinder"/>), and eagerly
    /// registers each discovered handler type as scoped.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="types">The candidate types to inspect for handler interfaces and <see cref="MessageAttribute"/>.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
        Type[] types)
    {
        // Record the scanned candidates (cumulatively, one record per call) so cross-cutting
        // diagnostics can see the types discovery skipped - e.g. Benzene.Http's
        // UnroutedHttpEndpointCheck flagging an [HttpEndpoint] handler missing its [Message].
        services.AddSingleton(new MessageHandlerCandidateTypes(types));

        var cacheMessageHandlersFinder = new CacheMessageHandlersFinder(new ReflectionMessageHandlersFinder(types));
        foreach (var handler in cacheMessageHandlersFinder.FindDefinitions())
        {
            services.AddScoped(handler.HandlerType);
        }

        services.TryAddSingleton<MessageHandlersList>();
        services.TryAddSingleton<DependencyMessageHandlersFinder>();
        services.TryAddSingleton<IMessageHandlersList>(x => x.GetService<MessageHandlersList>());
        services.TryAddSingleton<IMessageHandlersFinder>(x =>
            new CompositeMessageHandlersFinder(
                cacheMessageHandlersFinder,
            x.GetService<MessageHandlersList>(),
            x.GetService<DependencyMessageHandlersFinder>()
        ));
        services.TryAddSingleton<MessageHandlerDefinitionIndex>();

        services.TryAddScoped<IMessageHandlerDefinitionLookUp, MessageHandlerDefinitionLookUp>();
        services.TryAddScoped<IHandlerPipelineBuilder, HandlerPipelineBuilder>();
        services.TryAddScoped<IMessageHandlerWrapper, PipelineMessageHandlerWrapper>();
        services.TryAddScoped<IMessageHandlerFactory, MessageHandlerFactory>();
        services.TryAddScoped(typeof(MessageRouter<>));

        services.AddContextItems();
        return services;
    }
}
