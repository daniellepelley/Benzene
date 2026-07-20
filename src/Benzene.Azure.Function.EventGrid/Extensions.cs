using Benzene.Azure.Function.Core;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Provides extension methods for dispatching Event Grid trigger deliveries to a built
/// <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Dispatches Event Grid events to the Azure Function app's Event Grid entry point application.
    /// The trigger delivers one event per invocation by default; the <c>params</c> shape covers
    /// batched delivery and tests.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="events">The events to handle.</param>
    /// <returns>A task that completes when the events have been handled.</returns>
    public static Task HandleEventGridEvents(this IAzureFunctionApp source, params EventGridTriggerEvent[] events)
    {
        return source.HandleAsync(events);
    }

    /// <summary>
    /// Dispatches Event Grid events to the <paramref name="name"/>-keyed entry point - use when more
    /// than one Event Grid function is registered (each via <c>UseEventGrid(..., name: "fn")</c>).
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="name">The discriminator name matching the registered <c>UseEventGrid(..., name)</c>.</param>
    /// <param name="events">The events to handle.</param>
    /// <returns>A task that completes when the events have been handled.</returns>
    public static Task HandleEventGridEvents(this IAzureFunctionApp source, string name, params EventGridTriggerEvent[] events)
    {
        return source.HandleAsync(events, name);
    }

    /// <summary>
    /// Dispatches a raw Event Grid delivery - the <c>[EventGridTrigger] string</c> binding - to the
    /// Azure Function app's Event Grid entry point application, parsing either the Event Grid schema
    /// or CloudEvents 1.0 (see <see cref="EventGridTriggerEvent.Parse"/>).
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="eventJson">The event JSON as delivered to the trigger.</param>
    /// <returns>A task that completes when the event has been handled.</returns>
    public static Task HandleEventGridEvent(this IAzureFunctionApp source, string eventJson)
    {
        return source.HandleEventGridEvents(EventGridTriggerEvent.Parse(eventJson));
    }

    /// <summary>
    /// Dispatches a raw Event Grid delivery to the <paramref name="name"/>-keyed entry point - use
    /// when more than one Event Grid function is registered.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="name">The discriminator name matching the registered <c>UseEventGrid(..., name)</c>.</param>
    /// <param name="eventJson">The event JSON as delivered to the trigger.</param>
    /// <returns>A task that completes when the event has been handled.</returns>
    public static Task HandleEventGridEvent(this IAzureFunctionApp source, string name, string eventJson)
    {
        return source.HandleEventGridEvents(name, EventGridTriggerEvent.Parse(eventJson));
    }
}
