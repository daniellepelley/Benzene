namespace Benzene.Mesh.Reporting;

/// <summary>
/// Tracks when <see cref="MeshSelfReportMiddleware{TContext}"/> last published, so the throttle in
/// <see cref="MeshSelfReportOptions.MinimumInterval"/> survives across requests within one running
/// process. Registered as a singleton (<c>Extensions.AddMeshSelfReport</c>) - deliberately NOT
/// static, so tests (and any host running multiple independent pipelines in one process) don't
/// share state that should be process-local, not type-local. Public rather than internal purely so
/// <see cref="MeshSelfReportMiddleware{TContext}"/>'s public constructor can take it as a parameter
/// without an accessibility mismatch - not intended to be constructed directly outside
/// <c>Extensions.AddMeshSelfReport</c>'s DI registration.
/// </summary>
/// <remarks>
/// On an ephemeral host (e.g. AWS Lambda), this resets on every cold start - an idle Lambda
/// therefore simply stops reporting rather than reporting stale data forever, which is the accepted
/// tradeoff of opportunistic-only reporting (see the roadmap's still-open "staleness" note).
/// </remarks>
public class MeshSelfReportState
{
    /// <summary>When the middleware last published, or <c>null</c> if it never has.</summary>
    public DateTimeOffset? LastPublishedAtUtc { get; set; }
}
