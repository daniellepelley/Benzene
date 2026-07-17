namespace Benzene.Azure.Function.Timer;

/// <summary>
/// Benzene's own model of a timer trigger tick - dependency-free, shaped to bind directly from the
/// isolated worker's <c>TimerInfo</c> JSON (bind the trigger parameter as this type instead of the
/// extension package's <c>TimerInfo</c>, or map the two - the property names match).
/// </summary>
public class TimerTriggerInfo
{
    /// <summary>
    /// Whether this tick is running later than scheduled (e.g. after a host restart with
    /// <c>RunOnStartup</c> or a missed schedule).
    /// </summary>
    public bool IsPastDue { get; init; }

    /// <summary>The schedule bookkeeping for this timer, when the host provides it.</summary>
    public TimerScheduleStatus? ScheduleStatus { get; init; }
}

/// <summary>
/// The schedule bookkeeping the Functions host tracks for a timer - last/next occurrence and when
/// the status was last updated. Property names match the isolated worker's
/// <c>TimerScheduleStatus</c> JSON.
/// </summary>
public class TimerScheduleStatus
{
    /// <summary>The last recorded schedule occurrence.</summary>
    public DateTimeOffset? Last { get; init; }

    /// <summary>The expected next schedule occurrence.</summary>
    public DateTimeOffset? Next { get; init; }

    /// <summary>When the schedule status was last updated.</summary>
    public DateTimeOffset? LastUpdated { get; init; }
}
