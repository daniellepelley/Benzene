using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// Registration entry point for response events - republishing a handler's response payload as a
/// follow-up event (e.g. SQS <c>order:create</c> returning an <c>OrderCreated</c> payload that is
/// broadcast on <c>order:created</c>).
/// </summary>
public static class ResponseEventsExtensions
{
    /// <summary>
    /// Adds response-event publishing to this pipeline's handler dispatch: after a handler runs,
    /// every configured mapping that matches the (topic, result) pair publishes the response
    /// payload as an event through the registered <see cref="IResponseEventPublisher"/> (by
    /// default, <see cref="BenzeneMessageSenderResponseEventPublisher"/> over
    /// <c>IBenzeneMessageSender</c> - each event topic needs an <c>AddOutboundRouting</c> route).
    /// Scoped to the pipeline whose <c>UseMessageHandlers(router =&gt; ...)</c> call this is made
    /// in; other pipelines sharing the same handlers are unaffected.
    /// </summary>
    /// <param name="builder">The router builder passed to <c>UseMessageHandlers</c>'s configuration callback.</param>
    /// <param name="configure">Configures this pipeline's mappings and failure policy.</param>
    /// <returns>The same router builder, for chaining.</returns>
    public static IMessageRouterBuilder UseResponseEvents(this IMessageRouterBuilder builder, Action<ResponseEventsBuilder> configure)
    {
        var responseEventsBuilder = new ResponseEventsBuilder();
        configure(responseEventsBuilder);
        var mappings = responseEventsBuilder.Build();

        builder.Add(new ResponseEventsMiddlewareBuilder(mappings));
        builder.Register(services =>
        {
            services.AddSingleton(mappings);
            services.TryAddSingleton<ResponseEventCatalog>();
            services.TryAddSingleton<IResponseEventCatalog>(resolver => resolver.GetService<ResponseEventCatalog>());
            services.TryAddSingleton<IMessageDefinitionFinder<IMessageDefinition>>(resolver => resolver.GetService<ResponseEventCatalog>());
            services.TryAddScoped<IResponseEventPublisher, BenzeneMessageSenderResponseEventPublisher>();
        });

        return builder;
    }
}
