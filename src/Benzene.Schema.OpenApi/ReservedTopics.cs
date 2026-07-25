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
    /// <summary>The reserved topic ids Benzene itself declares, compared case-insensitively.
    /// Delegates to <see cref="Benzene.Abstractions.BenzeneTopic"/>, the single source of truth.</summary>
    public static IReadOnlyCollection<string> DefaultIds => Benzene.Abstractions.BenzeneTopic.All;

    /// <summary>
    /// Whether <paramref name="topic"/> is a framework-owned topic. Since the 2026-07-25 rename this
    /// is a <c>benzene:</c> prefix test rather than a name-list lookup, which also fixes a latent gap:
    /// the mesh wire topics (<c>benzene:mesh:register</c>, …) were never in the old list, so they were
    /// classified as domain topics.
    /// </summary>
    public static bool IsReserved(string? topic) => Benzene.Abstractions.BenzeneTopic.IsReserved(topic);
}
