namespace Benzene.HealthChecks.Core;

/// <summary>
/// How a client health check verifies its dependency. The default (<see cref="Reachability"/>) is a
/// non-destructive read-only probe safe to poll; <see cref="Active"/> opts in to exercising the real
/// write/invoke path and is side-effecting.
/// </summary>
public enum HealthCheckMode
{
    /// <summary>
    /// Non-destructive reachability: a read-only control-plane call (e.g. describe/get-attributes) that
    /// confirms the resource exists, is reachable, and the credentials can see it. The default — safe to
    /// poll at probe cadence. It does <b>not</b> prove that a write/invoke would succeed (which needs a
    /// different permission), only that the resource is reachable.
    /// </summary>
    Reachability = 0,

    /// <summary>
    /// Actively exercises the write/invoke path (send a message, invoke the function, start an
    /// execution). <b>Side-effecting</b> — opt in only. Keep it off liveness/readiness probes and off a
    /// frequent poll, and prefer a long cadence or an on-demand call. Reported under a distinct
    /// <c>"&lt;Type&gt;.Active"</c> check type so it never shares a cache key with the reachability check.
    /// </summary>
    Active = 1,
}
