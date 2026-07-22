using System;

namespace Benzene.Azure.Function.Timer;

/// <summary>
/// Declares a Timer-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so Benzene's source generator emits the <c>[Function]</c>/<c>[TimerTrigger]</c> class for you.
/// Place at assembly scope; multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.Timer</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneTimerTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-timer";

    /// <summary>The NCRONTAB schedule expression. Defaults to every 5 minutes (<c>0 */5 * * * *</c>).</summary>
    public string Schedule { get; set; } = "0 */5 * * * *";

    /// <summary>Whether to run once immediately on a cold start rather than waiting for the first tick.</summary>
    public bool RunOnStartup { get; set; }
}
