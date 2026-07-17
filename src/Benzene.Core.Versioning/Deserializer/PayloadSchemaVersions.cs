namespace Benzene.Core.Versioning.Deserializer;

public class PayloadSchemaVersions
{
    public required string Topic { get; init; }
    public required string[] FromSchemas { get; init; }
    public required string[] ToSchemas { get; init; }
}

