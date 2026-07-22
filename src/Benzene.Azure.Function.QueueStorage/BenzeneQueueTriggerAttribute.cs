using System;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Declares a Queue Storage-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so Benzene's source generator emits the <c>[Function]</c>/<c>[QueueTrigger]</c> class for you.
/// Place at assembly scope; multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneQueueTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-queue";

    /// <summary>The queue to trigger on.</summary>
    public string QueueName { get; set; } = "";

    /// <summary>The app-setting name holding the storage connection. Defaults to <c>AzureWebJobsStorage</c>.</summary>
    public string Connection { get; set; } = "AzureWebJobsStorage";
}
