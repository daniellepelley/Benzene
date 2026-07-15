using Benzene.HealthChecks.Core;

namespace Benzene.Mesh.Reporting;

/// <summary>
/// Configures <see cref="MeshSelfReportMiddleware{TContext}"/> - how to name this service, how to
/// obtain its own current spec/health on demand, and how often that's allowed to happen.
/// </summary>
/// <remarks>
/// Deliberately takes <paramref name="specProvider"/>/<paramref name="healthProvider"/> as
/// delegates rather than this package generating spec/health itself - a self-reporting service
/// already knows how to produce its own spec (<c>Benzene.Schema.OpenApi</c>) and health
/// (<c>Benzene.HealthChecks</c>), since it wired those up for its own primary purposes. Keeping
/// <c>Benzene.Mesh.Reporting</c> free of a dependency on either package is what keeps it light
/// enough to link into an arbitrary monitored service.
/// </remarks>
public class MeshSelfReportOptions
{
    /// <summary>Initializes a new instance of the <see cref="MeshSelfReportOptions"/> class.</summary>
    /// <param name="serviceName">This service's name (matches its registry entry's <c>Name</c>).</param>
    /// <param name="specProvider">Produces this service's current spec document, verbatim, or <c>null</c> if unavailable.</param>
    /// <param name="healthProvider">Produces this service's current aggregated health check response, or <c>null</c> if unavailable.</param>
    /// <param name="minimumInterval">
    /// The minimum time between opportunistic publishes - a publish is skipped if the last one was
    /// more recent than this, even if a real request/message just completed. Defaults to 5 minutes.
    /// No scheduled/cron-based publishing exists in this package by design - see its <c>CLAUDE.md</c>.
    /// </param>
    public MeshSelfReportOptions(
        string serviceName,
        Func<Task<string?>> specProvider,
        Func<Task<HealthCheckResponse?>> healthProvider,
        TimeSpan? minimumInterval = null)
    {
        ServiceName = serviceName;
        SpecProvider = specProvider;
        HealthProvider = healthProvider;
        MinimumInterval = minimumInterval ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>This service's name.</summary>
    public string ServiceName { get; }

    /// <summary>Produces this service's current spec document, verbatim, or <c>null</c> if unavailable.</summary>
    public Func<Task<string?>> SpecProvider { get; }

    /// <summary>Produces this service's current aggregated health check response, or <c>null</c> if unavailable.</summary>
    public Func<Task<HealthCheckResponse?>> HealthProvider { get; }

    /// <summary>The minimum time between opportunistic publishes.</summary>
    public TimeSpan MinimumInterval { get; }
}
