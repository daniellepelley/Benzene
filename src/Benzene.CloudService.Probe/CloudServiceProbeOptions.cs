namespace Benzene.CloudService.Probe;

/// <summary>
/// Configures a <see cref="CloudServiceProbe"/> run. Every path defaults to the profile's
/// /benzene/* standard (<see cref="CloudServiceProbePaths"/>); the target service itself is
/// <c>HttpClient.BaseAddress</c> on the client passed to <see cref="CloudServiceProbe.RunAsync"/>.
/// </summary>
public sealed class CloudServiceProbeOptions
{
    /// <summary>The wire-envelope endpoint to probe for R4/R6. Defaults to <see cref="CloudServiceProbePaths.Invoke"/>.</summary>
    public string InvokePath { get; set; } = CloudServiceProbePaths.Invoke;

    /// <summary>The derived spec endpoint to probe for R5. Defaults to <see cref="CloudServiceProbePaths.Spec"/>.</summary>
    public string SpecPath { get; set; } = CloudServiceProbePaths.Spec;

    /// <summary>The health endpoint to probe for R3. Defaults to <see cref="CloudServiceProbePaths.Health"/>.</summary>
    public string HealthPath { get; set; } = CloudServiceProbePaths.Health;

    /// <summary>
    /// When true (the default), the R4 and R6 probe requests carry a synthetic W3C
    /// <c>traceparent</c> header, and R8's reason notes whether the service still responded
    /// correctly with it attached. This is a weak, explicitly-labeled non-breakage signal only -
    /// it never upgrades R8 past <see cref="CloudServiceProbeVerdict.Inconclusive"/>, since actual
    /// propagation isn't observable from a single service. Set false to skip sending it.
    /// </summary>
    public bool SendTraceParentProbe { get; set; } = true;

    /// <summary>
    /// True when every path is still at its /benzene/* default. R7 can only be assessed when this
    /// holds - once the caller points the probe elsewhere, it can no longer tell whether the
    /// service's *own* defaults are /benzene/* (see <see cref="CloudServiceProbe"/>'s R7 check).
    /// </summary>
    internal bool UsesDefaultPaths =>
        InvokePath == CloudServiceProbePaths.Invoke &&
        SpecPath == CloudServiceProbePaths.Spec &&
        HealthPath == CloudServiceProbePaths.Health;
}
