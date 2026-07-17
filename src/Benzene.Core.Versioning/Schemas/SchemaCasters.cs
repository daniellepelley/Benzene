using System.Diagnostics.CodeAnalysis;

namespace Benzene.Core.Versioning.Schemas;

public class SchemaCasters : ISchemaCasters
{
    private readonly ISchemaCaster[] _schemaCastDefinitions;

    // Built once (this type is a singleton), so the per-message request/response casting lookups are
    // O(1) rather than a linear scan over every registered caster.
    private readonly Dictionary<(string Topic, string FromSchema, Type ToType), ISchemaCaster> _byFromSchemaAndToType = new();
    private readonly Dictionary<(string Topic, Type FromType, string ToSchema), ISchemaCaster> _byFromTypeAndToSchema = new();

    public SchemaCasters(IEnumerable<ISchemaCaster> schemaCastDefinitions)
    {
        _schemaCastDefinitions = schemaCastDefinitions.ToArray();

        foreach (var caster in _schemaCastDefinitions)
        {
            // First registration wins on a duplicate key; a well-formed set never has one.
            _ = _byFromSchemaAndToType.TryAdd((caster.Definition.Topic, caster.Definition.FromSchema, caster.ToType), caster);
            _ = _byFromTypeAndToSchema.TryAdd((caster.Definition.Topic, caster.FromType, caster.Definition.ToSchema), caster);
        }
    }

    public ISchemaCaster[] GetAll() => _schemaCastDefinitions;

    public ISchemaCaster GetSchemaCaster(string fromSchema, string toSchema, string topic)
    {
        return _schemaCastDefinitions.FirstOrDefault(d =>
            string.Equals(d.Definition.FromSchema, fromSchema, StringComparison.Ordinal) &&
            string.Equals(d.Definition.ToSchema, toSchema, StringComparison.Ordinal) &&
            string.Equals(d.Definition.Topic, topic, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"No conversion found for topic='{topic}' from '{fromSchema}' to '{toSchema}'.");
    }

    public bool TryGetSchemaCaster(string topic, string fromSchema, Type toType, [NotNullWhen(true)] out ISchemaCaster? caster)
    {
        return _byFromSchemaAndToType.TryGetValue((topic, fromSchema, toType), out caster);
    }

    public bool TryGetSchemaCaster(string topic, Type fromType, string toSchema, [NotNullWhen(true)] out ISchemaCaster? caster)
    {
        return _byFromTypeAndToSchema.TryGetValue((topic, fromType, toSchema), out caster);
    }
}
