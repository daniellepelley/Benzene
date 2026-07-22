namespace Benzene.HealthChecks.Core;

/// <summary>
/// A <see cref="IHealthCheck"/> registered under the <b>dependency</b> category: an external-dependency
/// reachability check (a queue, a downstream service, a database) that auto-wired clients contribute.
/// <para>
/// This is a <b>DI-registration category, not a context marker</b> — the concrete checks stay plain
/// <see cref="IHealthCheck"/> implementations; the auto-wiring registers them <em>as</em>
/// <see cref="IDependencyHealthCheck"/> (typically wrapped by <c>DependencyHealthCheck</c>).
/// </para>
/// <para>
/// <b>These belong on the deep <c>healthcheck</c> layer, not on a Kubernetes probe.</b> A dependency
/// check is <b>shared-fate</b>: every replica runs the same check against the same downstream, so a
/// transient blip fails all of them at once. Wiring that into <b>liveness</b> would restart-storm the
/// fleet; wiring it into <b>readiness</b> would pull <em>every</em> pod from the Service's endpoints at
/// once, turning a degraded dependency into a total outage (callers get connection-refused instead of a
/// structured 503) — the classic cascading-failure anti-pattern. So the general (<c>healthcheck</c>)
/// probe harvests these for monitoring / the mesh inventory / humans, while <c>liveness</c>,
/// <c>readiness</c> and <c>contracts</c> do not. A developer who has reasoned that a specific dependency
/// is genuinely safe to gate traffic on can still add it to readiness explicitly
/// (<c>.UseReadinessCheck(b =&gt; b.AddSqsHealthCheck(...))</c>); auto-wiring never does it for them.
/// </para>
/// </summary>
public interface IDependencyHealthCheck : IHealthCheck
{
    /// <summary>
    /// A stable key used to de-duplicate dependency checks so two registrations of the same dependency
    /// (e.g. two <c>.UseSns(sameArn)</c> calls) collapse to a single check. Phase 1 auto-wiring sets this
    /// to the dependency's <c>(Type, Name)</c>. Defaults to the check's <see cref="IHealthCheck.Type"/>.
    /// </summary>
    string DedupKey => Type;
}
