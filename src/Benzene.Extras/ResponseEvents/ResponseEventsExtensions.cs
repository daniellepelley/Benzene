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
            services.TryAddScoped<IResponseEventPublisher, BenzeneMessageSenderResponseEventPublisher>();
            AddResponseEventCatalog(services);
        });

        return builder;
    }

    /// <summary>
    /// Declares published events that don't come from a response mapping - events handler code
    /// sends directly via <c>IBenzeneMessageSender</c> - so they still appear in generated specs
    /// (AsyncAPI / event-service documents) and in <see cref="IResponseEventCatalog"/>. Purely
    /// declarative: registers no runtime behavior.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="definitions">The (event topic, payload type) definitions this service publishes,
    /// e.g. <see cref="ResponseEventDefinition"/> instances.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddResponseEventDeclarations(this IBenzeneServiceContainer services, params IMessageDefinition[] definitions)
    {
        services.AddSingleton(new ResponseEventDeclarations(definitions));
        AddResponseEventCatalog(services);
        return services;
    }

    private static void AddResponseEventCatalog(IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<ResponseEventCatalog>();
        services.TryAddSingleton<IResponseEventCatalog>(resolver => resolver.GetService<ResponseEventCatalog>());
        services.TryAddSingleton<IMessageDefinitionFinder<IMessageDefinition>>(resolver => resolver.GetService<ResponseEventCatalog>());
    }
}
