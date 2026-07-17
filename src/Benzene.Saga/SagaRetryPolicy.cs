namespace Benzene.Saga;

/// <summary>
/// An optional whole-saga retry policy: after a <em>clean</em> rollback (the system is back at its
/// starting state), re-run the entire saga up to <see cref="MaxAttempts"/> times with exponential
/// backoff. Retry is deliberately limited to <see cref="SagaOutcome.RolledBack"/> — a succeeded saga
/// needs no retry, and a <see cref="SagaOutcome.PartiallyRolledBack"/> one may have left effects a
/// retry would double-apply, so it is surfaced for manual attention instead.
/// </summary>
public class SagaRetryPolicy
{
    /// <summary>Initializes a retry policy.</summary>
    /// <param name="maxAttempts">The total number of attempts (1 = no retry). Must be at least 1.</param>
    /// <param name="initialDelay">The delay before the second attempt. Defaults to none.</param>
    /// <param name="backoffFactor">The multiplier applied to the delay after each attempt. Defaults to 2.0.</param>
    /// <param name="delay">A delay function, overridable for tests. Defaults to <see cref="Task.Delay(TimeSpan)"/>.</param>
    public SagaRetryPolicy(
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        double backoffFactor = 2.0,
        Func<TimeSpan, Task>? delay = null)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "A saga must be attempted at least once.");
        }

        MaxAttempts = maxAttempts;
        InitialDelay = initialDelay ?? TimeSpan.Zero;
        BackoffFactor = backoffFactor;
        Delay = delay ?? Task.Delay;
    }

    /// <summary>Gets the total number of attempts (1 = no retry).</summary>
    public int MaxAttempts { get; }

    /// <summary>Gets the delay before the second attempt.</summary>
    public TimeSpan InitialDelay { get; }

    /// <summary>Gets the multiplier applied to the delay after each attempt.</summary>
    public double BackoffFactor { get; }

    /// <summary>Gets the delay function used between attempts.</summary>
    internal Func<TimeSpan, Task> Delay { get; }
}
