using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Google.Events.Protobuf.Cloud.PubSub.V1;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Provides extension methods for registering Pub/Sub message-handling services and adding Pub/Sub
/// trigger handling to a Google Cloud Functions host.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process a Pub/Sub-triggered message.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UsePubSub"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddGooglePubSub(this IBenzeneServiceContainer services)
        => services.AddGooglePubSub(PubSubMessageTopicGetter.DefaultTopicAttribute);

    /// <summary>
    /// Registers the services required to process a Pub/Sub-triggered message, with the topic getter
    /// reading the given message-attribute key (see <see cref="PubSubMessageTopicGetter.DefaultTopicAttribute"/>).
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is read from.</param>
    /// <returns>The service container, for method chaining.</returns>
    public static IBenzeneServiceContainer AddGooglePubSub(this IBenzeneServiceContainer services, string topicAttributeKey)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<PubSubContext>>(_ => new PubSubMessageTopicGetter(topicAttributeKey));
        services.AddScoped<IMessageVersionGetter<PubSubContext>, HeaderMessageVersionGetter<PubSubContext>>();
        services.AddScoped<IMessageHeadersGetter<PubSubContext>, PubSubMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<PubSubContext>, PubSubMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<PubSubContext>, PubSubMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.PubSub));
        return services;
    }

    /// <summary>
    /// Applies Pub/Sub-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than a Google Cloud Functions Pub/Sub host (i.e. any
    /// <see cref="BenzeneStartUp"/> hosted via <see cref="GooglePubSubFunctionHost{TStartUp}"/>).
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the Pub/Sub middleware pipeline.</param>
    /// <param name="configure">
    /// Optionally configures <see cref="PubSubOptions"/> - e.g. set <see cref="PubSubOptions.CatchExceptions"/>
    /// to contain a handler exception instead of the default cascade-to-invocation-failure behavior,
    /// or <see cref="PubSubOptions.RaiseOnFailureStatus"/> to escalate a non-exception failure result
    /// into a thrown exception too.
    /// </param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UsePubSub(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<PubSubContext>> action, Action<PubSubOptions>? configure = null, string topicAttributeKey = PubSubMessageTopicGetter.DefaultTopicAttribute)
    {
        if (app is GooglePubSubFunctionApplicationBuilder pubSubApp)
        {
            app.Register(x => x.AddGooglePubSub(topicAttributeKey));
            var pipeline = app.Create<PubSubContext>();
            action(pipeline);
            var options = new PubSubOptions();
            configure?.Invoke(options);
            pubSubApp.Add(serviceResolverFactory => new EntryPointMiddlewareApplication<MessagePublishedData>(
                new PubSubMiddlewareApplication(pipeline.Build(), options), serviceResolverFactory));
        }
        return app;
    }
}
