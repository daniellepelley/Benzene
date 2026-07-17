namespace Benzene.CloudService.Probe;

/// <summary>
/// The default service standard's well-known HTTP paths (docs/specification/design-principles.md
/// §5.2, mandatory-by-default under the Cloud Service Profile's R7).
///
/// These three string literals deliberately duplicate <c>Benzene.CloudService.CloudServicePaths</c>
/// rather than referencing it: this package must stay usable against ANY Benzene Cloud Service
/// reachable over HTTP - including non-.NET ports, since the profile is a language-neutral spec -
/// so it cannot take a project reference on the .NET wiring package it exists to independently
/// audit. Do not "fix" this into a shared reference; the duplication is the point.
/// </summary>
public static class CloudServiceProbePaths
{
    /// <summary>The wire-envelope endpoint (profile R4).</summary>
    public const string Invoke = "/benzene/invoke";

    /// <summary>The derived spec document (profile R5).</summary>
    public const string Spec = "/benzene/spec";

    /// <summary>The aggregated health check response (profile R3).</summary>
    public const string Health = "/benzene/health";
}
