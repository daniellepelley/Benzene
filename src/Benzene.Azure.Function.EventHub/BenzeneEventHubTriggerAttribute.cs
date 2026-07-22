using System;

namespace Benzene.Azure.Function.EventHub;

/// <summary>
/// Declares an Event Hubs-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so Benzene's source generator emits the <c>[Function]</c>/<c>[EventHubTrigger]</c> class for you.
/// Place at assembly scope; multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.EventHubs</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneEventHubTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-event-hub";

    /// <summary>The Event Hub name to trigger on.</summary>
    public string EventHubName { get; set; } = "";

    /// <summary>The app-setting name holding the Event Hubs connection. Defaults to <c>EventHubConnection</c>.</summary>
    public string Connection { get; set; } = "EventHubConnection";

    /// <summary>The consumer group. Optional; omit for the default consumer group.</summary>
    public string ConsumerGroup { get; set; } = "";
}
