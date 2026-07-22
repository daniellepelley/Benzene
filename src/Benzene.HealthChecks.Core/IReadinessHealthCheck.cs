namespace Benzene.HealthChecks.Core;

/// <summary>
/// A <see cref="IHealthCheck"/> registered under the <b>readiness</b> category: an external-dependency
/// reachability check (a queue, a downstream service, a database) that auto-wired clients contribute.
/// <para>
/// This is a <b>DI-registration category, not a context marker</b> — the concrete checks stay plain
/// <see cref="IHealthCheck"/> implementations; the auto-wiring registers them <em>as</em>
/// <see cref="IReadinessHealthCheck"/> (typically wrapped by <c>ReadinessHealthCheck</c>). The point is
/// the one-way-door discipline of §3.2: a dependency being down is a <b>readiness</b> concern (stop
/// routing traffic), never a <b>liveness</b> concern (restart the pod). So the general (<c>healthcheck</c>)
/// and <c>readiness</c> probes harvest these, while the <c>liveness</c> and <c>contracts</c> probes do
/// not — a transient downstream blip must not restart-storm the fleet.
/// </para>
/// </summary>
public interface IReadinessHealthCheck : IHealthCheck
{
    /// <summary>
    /// A stable key used to de-duplicate readiness checks so two registrations of the same dependency
    /// (e.g. two <c>.UseSns(sameArn)</c> calls) collapse to a single check. Phase 1 auto-wiring sets this
    /// to the dependency's <c>(Type, Name)</c>. Defaults to the check's <see cref="IHealthCheck.Type"/>.
    /// </summary>
    string DedupKey => Type;
}
