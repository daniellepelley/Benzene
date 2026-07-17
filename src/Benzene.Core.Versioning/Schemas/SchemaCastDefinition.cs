namespace Benzene.Core.Versioning.Schemas;

public class SchemaCastDefinition
{
    public required string Topic { get; init; }
    public required string FromSchema { get; init; }
    public required string ToSchema { get; init; }
    public override string ToString() => $"{Topic}: {FromSchema} -> {ToSchema}";
}
