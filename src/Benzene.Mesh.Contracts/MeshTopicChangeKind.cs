namespace Benzene.Mesh.Contracts;

/// <summary>
/// The kinds of run-over-run topic catalog change <c>Benzene.Mesh.Aggregator</c> detects - loose
/// string constants (the <see cref="MeshServiceStatus"/> convention, not an enum) so an older
/// reader renders an unknown kind's description rather than failing.
/// </summary>
public static class MeshTopicChangeKind
{
    /// <summary>The (topic, version) was not declared anywhere in the previous run.</summary>
    public const string Added = "topic-added";

    /// <summary>The topic's payload schema (request, response, or message side) changed since the previous run.</summary>
    public const string SchemaChanged = "schema-changed";

    /// <summary>The set of services declaring they produce this topic changed since the previous run.</summary>
    public const string ProducersChanged = "producers-changed";

    /// <summary>The set of services handling this topic changed since the previous run.</summary>
    public const string ConsumersChanged = "consumers-changed";
}
