using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Benzene.Mesh.Wire;

/// <summary>
/// The mesh ServiceDescriptor wire shape (docs/specification/mesh.md §2): the service's
/// self-description, derived at startup from the message-handler registry - never hand-maintained.
/// Also the body of a <c>mesh:register</c> message (§4). Wire field names are camelCase; use
/// <see cref="MeshJson.Options"/> (or any camelCase serializer) on the wire.
/// </summary>
public class MeshServiceDescriptor
{
    public string Service { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceVersion { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceId { get; set; }

    public string Runtime { get; set; } = "dotnet";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Binding { get; set; }

    public MeshPlacement Placement { get; set; } = new();

    public List<MeshTopicDescriptor> Topics { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescriptorHash { get; set; }

    /// <summary>
    /// Names the feeds that were unavailable when the descriptor was built (spec §2: currently only
    /// "registry"), so a reduced descriptor is distinguishable from a service with no topics.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Degraded { get; set; }

    /// <summary>
    /// The service's self-assessed conformance profile (spec §2), when it claims one - e.g. the
    /// Cloud Service Profile (docs/specification/cloud-service-profile.md). Optional; omitted by
    /// services that don't self-assess. Like <see cref="Degraded"/>, this is self-description
    /// status rather than contract, so it is excluded from the descriptor hash.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeshProfile? Profile { get; set; }
}

/// <summary>
/// A named conformance-profile claim carried on the descriptor (spec §2's <c>profile</c> field).
/// <see cref="Missing"/> lists the profile's requirement ids the service knows it does not satisfy;
/// null or empty means the service self-assesses as fully conformant to <see cref="Name"/>.
/// </summary>
public class MeshProfile
{
    public string Name { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Missing { get; set; }
}

/// <summary>One registered topic in a descriptor (spec §2), with the §2.1-derived payload schemas.</summary>
public class MeshTopicDescriptor
{
    public string Id { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonObject? RequestSchema { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonObject? ResponseSchema { get; set; }
}

/// <summary>Where a service instance runs (spec §2). Region is emitted only when actually known.</summary>
public class MeshPlacement
{
    public string Cloud { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Region { get; set; }
}
