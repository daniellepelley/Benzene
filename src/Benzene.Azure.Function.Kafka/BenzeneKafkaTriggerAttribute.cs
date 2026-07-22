using System;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Declares a Kafka-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so Benzene's source generator emits the <c>[Function]</c>/<c>[KafkaTrigger]</c> class for you.
/// Place at assembly scope; multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.Kafka</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneKafkaTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-kafka";

    /// <summary>The broker list — a raw host list or the app-setting name holding it. Defaults to <c>BrokerList</c>.</summary>
    public string BrokerList { get; set; } = "BrokerList";

    /// <summary>The topic to consume.</summary>
    public string Topic { get; set; } = "";

    /// <summary>The consumer group. Optional.</summary>
    public string ConsumerGroup { get; set; } = "";
}
