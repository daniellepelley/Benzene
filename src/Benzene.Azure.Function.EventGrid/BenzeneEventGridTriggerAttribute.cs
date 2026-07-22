using System;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Declares an Event Grid-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so Benzene's source generator emits the <c>[Function]</c>/<c>[EventGridTrigger]</c> class for you.
/// The event binds as a JSON string that Benzene parses (both the Event Grid schema and CloudEvents 1.0).
/// Place at assembly scope; multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.EventGrid</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneEventGridTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-event-grid";
}
