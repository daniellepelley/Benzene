using System;
using System.Collections.Generic;

namespace Benzene.Schema.OpenApi;

/// <summary>
/// The reserved "utility" topics of the Benzene Cloud Service Profile
/// (docs/specification/cloud-service-profile.md) — operational surfaces (spec, health, mesh
/// descriptor, envelope, self-report) that every conformant service exposes but which are not part
/// of its business domain. Spec and mesh tooling use this to separate them from a service's domain
/// topics (e.g. hide them behind a "show utilities" toggle).
/// </summary>
/// <remarks>
/// Matched by topic id against the profile's default ids. A service that renames a reserved topic
/// off its default (an unusual choice) won't be auto-classified — the domain/utility split is a
/// presentation aid, not a security boundary.
/// </remarks>
public static class ReservedTopics
{
    /// <summary>The default reserved topic ids, compared case-insensitively.</summary>
    public static readonly IReadOnlyCollection<string> DefaultIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Constants.DefaultSpecTopic, // "spec"
        "healthcheck",
        "liveness",
        "readiness",
        "mesh",     // cloud-service descriptor topic
        "invoke",   // wire-envelope endpoint topic
        "report",   // mesh self-report ingestion topic
    };

    private static readonly HashSet<string> Set = new(DefaultIds, StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether <paramref name="topic"/> is one of the reserved utility topics.</summary>
    public static bool IsReserved(string? topic) => topic != null && Set.Contains(topic);
}
