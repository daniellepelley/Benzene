namespace Benzene.Saga;

/// <summary>
/// Options for a saga run: an optional durable <see cref="ISagaStateStore"/> and an optional
/// <see cref="SagaRetryPolicy"/>. With no options (<see cref="Saga.RunAsync()"/>), the saga runs once
/// in-process with no recording — the default, zero-overhead behavior.
/// </summary>
public class SagaRunOptions
{
    /// <summary>
    /// The saga instance id, stable across retry attempts and used in every store call. Left unset,
    /// a new id is generated when a <see cref="StateStore"/> is present.
    /// </summary>
    public string? SagaId { get; set; }

    /// <summary>A human-readable saga name for grouping/reporting in the store. Defaults to <c>"saga"</c>.</summary>
    public string Name { get; set; } = "saga";

    /// <summary>An optional store recording progress and outcome. Null (the default) records nothing.</summary>
    public ISagaStateStore? StateStore { get; set; }

    /// <summary>An optional whole-saga retry policy. Null (the default) runs a single attempt.</summary>
    public SagaRetryPolicy? RetryPolicy { get; set; }
}
