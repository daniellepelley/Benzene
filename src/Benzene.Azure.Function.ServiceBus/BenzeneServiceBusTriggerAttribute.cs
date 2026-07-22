using System;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Declares a Service Bus-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so Benzene's source generator emits the <c>[Function]</c>/<c>[ServiceBusTrigger]</c> class for you.
/// Set either <see cref="QueueName"/> (queue trigger) or <see cref="TopicName"/>+<see cref="SubscriptionName"/>
/// (topic trigger). Place at assembly scope; multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.ServiceBus</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneServiceBusTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-service-bus";

    /// <summary>The queue to trigger on. Set this OR <see cref="TopicName"/>+<see cref="SubscriptionName"/>.</summary>
    public string QueueName { get; set; } = "";

    /// <summary>The topic to trigger on (with <see cref="SubscriptionName"/>). Alternative to <see cref="QueueName"/>.</summary>
    public string TopicName { get; set; } = "";

    /// <summary>The subscription to trigger on, when using <see cref="TopicName"/>.</summary>
    public string SubscriptionName { get; set; } = "";

    /// <summary>The app-setting name holding the Service Bus connection. Defaults to <c>ServiceBusConnection</c>.</summary>
    public string Connection { get; set; } = "ServiceBusConnection";
}
