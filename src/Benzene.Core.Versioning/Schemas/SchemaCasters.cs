namespace Benzene.Core.Versioning.Schemas;

public class SchemaCasters(IEnumerable<ISchemaCaster> schemaCastDefinitions) : ISchemaCasters
{
    private readonly ISchemaCaster[] _schemaCastDefinitions = schemaCastDefinitions.ToArray();

    public ISchemaCaster[] GetAll() => _schemaCastDefinitions;

    public ISchemaCaster GetSchemaCaster(string fromSchema, string toSchema, string topic)
    {
        return _schemaCastDefinitions.FirstOrDefault(d =>
            string.Equals(d.Definition.FromSchema, fromSchema, StringComparison.Ordinal) &&
            string.Equals(d.Definition.ToSchema, toSchema, StringComparison.Ordinal) &&
            string.Equals(d.Definition.Topic, topic, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"No conversion found for topic='{topic}' from '{fromSchema}' to '{toSchema}'.");
    }
}
