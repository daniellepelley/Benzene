using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using RabbitMQ.Client;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.RabbitMq.RabbitMqSendMessage;

/// <summary>
/// Pipeline-builder extensions for the outbound RabbitMQ publish path.
/// </summary>
public static class Extensions
{
    /// <summary>Adds the RabbitMQ publish middleware to an outbound pipeline.</summary>
    /// <param name="app">The outbound pipeline builder.</param>
    /// <param name="channel">The RabbitMQ channel to publish on.</param>
    /// <param name="mandatory">Whether an unroutable message is returned rather than dropped.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<RabbitMqSendMessageContext> UseRabbitMqClient(
        this IMiddlewarePipelineBuilder<RabbitMqSendMessageContext> app, IChannel channel, bool mandatory = false)
    {
        return app.Use(_ => new RabbitMqClientMiddleware(channel, mandatory));
    }

    /// <summary>
    /// Converts a Benzene outbound client context to a RabbitMQ publish and runs it through the given
    /// inner pipeline - the <c>OutboundRoutingBuilder</c> integration point, mirroring <c>UseKafka</c>.
    /// </summary>
    /// <typeparam name="T">The request message type.</typeparam>
    /// <param name="app">The outbound client pipeline builder.</param>
    /// <param name="exchange">The exchange to publish to (empty string for the default exchange).</param>
    /// <param name="action">Configures the inner RabbitMQ publish pipeline.</param>
    /// <param name="topicHeaderKey">
    /// The message-property header the topic is written to. Defaults to
    /// <see cref="RabbitMqConstants.DefaultTopicHeader"/> (<c>"topic"</c>); pass a different key to
    /// publish for a consumer that routes on another header.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseRabbitMq<T>(
        this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, string exchange,
        Action<IMiddlewarePipelineBuilder<RabbitMqSendMessageContext>> action,
        string topicHeaderKey = RabbitMqConstants.DefaultTopicHeader)
    {
        var converter = new RabbitMqContextConverter<T>(new Benzene.Core.MessageHandlers.Serialization.JsonSerializer(), exchange, topicHeaderKey);
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(serviceResolver => new ContextConverterMiddleware<IBenzeneClientContext<T, Void>, RabbitMqSendMessageContext>(converter, middlewarePipeline, serviceResolver));
    }

    /// <summary>
    /// Converts a Benzene outbound client context to a RabbitMQ publish over the given channel.
    /// </summary>
    /// <typeparam name="T">The request message type.</typeparam>
    /// <param name="app">The outbound client pipeline builder.</param>
    /// <param name="channel">The RabbitMQ channel to publish on.</param>
    /// <param name="exchange">The exchange to publish to (empty string for the default exchange).</param>
    /// <param name="topicHeaderKey">
    /// The message-property header the topic is written to. Defaults to
    /// <see cref="RabbitMqConstants.DefaultTopicHeader"/> (<c>"topic"</c>).
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseRabbitMq<T>(
        this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app, IChannel channel, string exchange = "",
        string topicHeaderKey = RabbitMqConstants.DefaultTopicHeader)
    {
        return app.UseRabbitMq<T>(exchange, builder => builder.UseRabbitMqClient(channel), topicHeaderKey);
    }
}
