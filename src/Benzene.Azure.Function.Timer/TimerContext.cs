using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;

namespace Benzene.Azure.Function.Timer;

/// <summary>
/// Provides the middleware pipeline context for a single timer tick within an Azure Functions timer
/// trigger invocation.
/// </summary>
public class TimerContext : IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimerContext"/> class.
    /// </summary>
    /// <param name="timer">The tick's timer information.</param>
    public TimerContext(TimerTriggerInfo timer)
    {
        Timer = timer;
    }

    /// <summary>
    /// Gets the tick's timer information.
    /// </summary>
    public TimerTriggerInfo Timer { get; }

    /// <summary>
    /// Gets or sets the result of handling this tick. A timer tick has no caller to answer, so this
    /// is recorded for middleware/diagnostics only.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;
}
