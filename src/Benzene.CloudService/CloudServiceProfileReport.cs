using Benzene.Mesh.Wire;

namespace Benzene.CloudService;

/// <summary>One requirement of the Cloud Service Profile (docs/specification/cloud-service-profile.md §2), as self-assessed at wire-up.</summary>
public sealed class CloudServiceRequirement
{
    public CloudServiceRequirement(string id, string description, bool satisfied, string? note = null)
    {
        Id = id;
        Description = description;
        Satisfied = satisfied;
        Note = note;
    }

    /// <summary>The requirement id (R1–R8).</summary>
    public string Id { get; }

    /// <summary>What the requirement mandates, in one line.</summary>
    public string Description { get; }

    /// <summary>Whether this service's wiring satisfies the requirement.</summary>
    public bool Satisfied { get; }

    /// <summary>Why the requirement is (or isn't) satisfied, when that needs saying.</summary>
    public string? Note { get; }
}

/// <summary>
/// The wiring-time self-assessment of a service against the Cloud Service Profile
/// (docs/specification/cloud-service-profile.md). Produced by <c>UseBenzeneCloudService</c> from
/// what was actually wired - defaults kept, surfaces declined, paths relocated - and carried on the
/// service's descriptor (mesh.md §2's <c>profile</c> field) so any tool that can reach the reserved
/// <c>mesh</c> topic can ask a running service whether it claims the profile.
///
/// This is a self-assessment of provisioning, not a runtime probe: it reflects what the builder
/// wired, and runtime degradation (an unreachable collector, a failing check) never changes it -
/// per the profile's §4, runtime degradation is not a conformance failure.
/// </summary>
public sealed class CloudServiceProfileReport
{
    /// <summary>The profile name carried on the wire (mesh.md §2 <c>profile.name</c>).</summary>
    public const string ProfileName = "cloud-service";

    private CloudServiceProfileReport(IReadOnlyList<CloudServiceRequirement> requirements)
    {
        Requirements = requirements;
    }

    /// <summary>Every profile requirement with its assessment, in R1–R8 order.</summary>
    public IReadOnlyList<CloudServiceRequirement> Requirements { get; }

    /// <summary>The ids of the requirements this service's wiring does not satisfy; empty when conformant.</summary>
    public IReadOnlyList<string> Missing => Requirements.Where(x => !x.Satisfied).Select(x => x.Id).ToArray();

    /// <summary>True when every requirement is satisfied - the service claims the profile.</summary>
    public bool IsConformant => Requirements.All(x => x.Satisfied);

    /// <summary>The wire shape for the descriptor's <c>profile</c> field.</summary>
    public MeshProfile ToMeshProfile()
    {
        var missing = Missing;
        return new MeshProfile
        {
            Name = ProfileName,
            Missing = missing.Count == 0 ? null : missing.ToList()
        };
    }

    internal static CloudServiceProfileReport Evaluate(CloudServiceBuilder builder)
    {
        var mesh = builder.MeshEnabled;
        var collector = builder.CollectorEnvelopeUrl != null;

        return new CloudServiceProfileReport(new[]
        {
            new CloudServiceRequirement("R1", "Hosted middleware pipeline", true,
                "wired by UseBenzeneCloudService on a hosted HTTP pipeline"),
            new CloudServiceRequirement("R2", "Message handlers via the registry", true,
                builder.HandlerTypes != null
                    ? $"{builder.HandlerTypes.Length} handler type(s) registered explicitly"
                    : "handlers taken from the container's registrations; the descriptor's topics are the runtime truth"),
            new CloudServiceRequirement("R3", "Health checks (reserved topic + HTTP surface)", true,
                builder.HealthChecks.Count == 0 ? "no checks added; the aggregate reports healthy" : null),
            new CloudServiceRequirement("R4", "Wire-envelope invocability", true),
            new CloudServiceRequirement("R5", "Derived spec", true),
            new CloudServiceRequirement("R6", "Mesh service-side feeds", mesh && collector,
                !mesh ? "mesh declined via WithoutMesh()"
                    : !collector ? "no collector configured: register/heartbeat/trace feeds have no destination"
                    : null),
            new CloudServiceRequirement("R7", "Default service standard paths", builder.UsesDefaultPaths,
                builder.UsesDefaultPaths ? null : "one or more surfaces relocated from their /benzene/ defaults; fleet tooling assumes the defaults"),
            new CloudServiceRequirement("R8", "Trace context join and propagation", mesh,
                mesh ? null : "mesh declined via WithoutMesh(): the trace middleware that joins traceparent is not wired")
        });
    }
}
