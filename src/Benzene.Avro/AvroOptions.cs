namespace Benzene.Avro;

/// <summary>
/// Configures how <see cref="AvroSerializer"/> obtains an Avro schema for a CLR type. Avro is
/// schema-based (unlike JSON/XML), so every type serialized needs a schema. Two modes are supported
/// and can be mixed:
/// <list type="bullet">
/// <item><b>Reflection (schemaless from the caller's perspective)</b> — the schema is inferred from the
/// CLR type's public read/write properties. On by default (<see cref="UseReflectionSchemas"/>).</item>
/// <item><b>Explicit schema</b> — an Avro schema (<c>.avsc</c> JSON) registered per type via
/// <see cref="RegisterSchema{T}"/>, matching the schema-registry model common in finance/Kafka
/// deployments. An explicit registration always wins over reflection for that type.</item>
/// </list>
/// </summary>
public class AvroOptions
{
    private readonly Dictionary<Type, string> _explicitSchemas = new();

    /// <summary>
    /// Whether to infer an Avro schema by reflection for any type that has no explicit schema
    /// registered. Defaults to <c>true</c>. Set to <c>false</c> to require an explicit schema for
    /// every serialized type (a type with no registration then throws).
    /// </summary>
    public bool UseReflectionSchemas { get; set; } = true;

    /// <summary>Registers an explicit Avro schema (<c>.avsc</c> JSON) for <paramref name="type"/>.</summary>
    /// <param name="type">The CLR type the schema applies to.</param>
    /// <param name="avroSchemaJson">The Avro schema as JSON.</param>
    /// <returns>These options, for chaining.</returns>
    public AvroOptions RegisterSchema(Type type, string avroSchemaJson)
    {
        _explicitSchemas[type] = avroSchemaJson;
        return this;
    }

    /// <summary>Registers an explicit Avro schema (<c>.avsc</c> JSON) for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The CLR type the schema applies to.</typeparam>
    /// <param name="avroSchemaJson">The Avro schema as JSON.</param>
    /// <returns>These options, for chaining.</returns>
    public AvroOptions RegisterSchema<T>(string avroSchemaJson) => RegisterSchema(typeof(T), avroSchemaJson);

    /// <summary>Gets the explicitly-registered schemas, keyed by CLR type.</summary>
    internal IReadOnlyDictionary<Type, string> ExplicitSchemas => _explicitSchemas;
}
