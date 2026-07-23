using System;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Google.Cloud.PubSub.V1;

namespace Benzene.Clients.GoogleCloud.PubSub;

/// <summary>
/// Provides extension methods for wiring Pub/Sub publishing into outbound routing pipelines — the
/// Google Cloud counterpart of <c>Benzene.Clients.Aws.Sqs</c>'s <c>UseSqs(...)</c>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds a <see cref="PubSubClientMiddleware"/> built from the given publisher to the pipeline.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="publisher">The Pub/Sub publisher API client used to publish messages.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<PubSubSendMessageContext> UsePubSubClient(
        this IMiddlewarePipelineBuilder<PubSubSendMessageContext> app, PublisherServiceApiClient publisher)
    {
        return app.Use(_ => new PubSubClientMiddleware(publisher));
    }

    /// <summary>
    /// Adds a <see cref="PubSubClientMiddleware"/> resolved from the service container to the pipeline.
    /// A <see cref="PublisherServiceApiClient"/> must be registered in DI.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<PubSubSendMessageContext> UsePubSubClient(
        this IMiddlewarePipelineBuilder<PubSubSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<PubSubClientMiddleware>());
        return app.Use<PubSubSendMessageContext, PubSubClientMiddleware>();
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to publish via Pub/Sub,
    /// using a custom send-pipeline configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="topic">The Pub/Sub topic to publish to (full resource path or bare topic id).</param>
    /// <param name="action">A callback used to configure the converted Pub/Sub send pipeline.</param>
    /// <param name="topicAttributeKey">The message attribute the routing topic is written to (defaults to <see cref="OutboundPubSubContextConverter.DefaultTopicAttribute"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UsePubSub(this IMiddlewarePipelineBuilder<OutboundContext> app,
        string topic,
        Action<IMiddlewarePipelineBuilder<PubSubSendMessageContext>> action,
        string topicAttributeKey = OutboundPubSubContextConverter.DefaultTopicAttribute)
    {
        return app.Convert(new OutboundPubSubContextConverter(topic, topicAttributeKey), action);
    }

    /// <summary>
    /// Converts an outbound route pipeline (<c>OutboundRoutingBuilder.Route</c>) to publish via Pub/Sub,
    /// using the default <see cref="PubSubClientMiddleware"/> configuration.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to convert.</param>
    /// <param name="topic">The Pub/Sub topic to publish to (full resource path or bare topic id).</param>
    /// <param name="topicAttributeKey">The message attribute the routing topic is written to (defaults to <see cref="OutboundPubSubContextConverter.DefaultTopicAttribute"/>).</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UsePubSub(this IMiddlewarePipelineBuilder<OutboundContext> app,
        string topic, string topicAttributeKey = OutboundPubSubContextConverter.DefaultTopicAttribute)
    {
        return app.Convert(new OutboundPubSubContextConverter(topic, topicAttributeKey), builder => builder.UsePubSubClient());
    }
}
