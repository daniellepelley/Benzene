namespace Benzene.CloudService;

/// <summary>
/// The default service standard's well-known HTTP paths (docs/specification/design-principles.md §5,
/// made mandatory-by-default by the Cloud Service Profile's R7). Every path remains configurable via
/// <see cref="ICloudServiceBuilder"/>, but a deployment that relocates a surface accepts the
/// documented degradation - fleet tooling assumes these defaults.
/// </summary>
public static class CloudServicePaths
{
    /// <summary>The wire-envelope endpoint (profile R4): <c>{topic, headers, body}</c> in, <c>{statusCode, headers, body}</c> out.</summary>
    public const string Invoke = "/benzene/invoke";

    /// <summary>The derived spec document (profile R5).</summary>
    public const string Spec = "/benzene/spec";

    /// <summary>The aggregated health check response (profile R3).</summary>
    public const string Health = "/benzene/health";
}
