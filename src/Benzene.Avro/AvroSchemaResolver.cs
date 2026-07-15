using System.Collections.Concurrent;
using Avro;

namespace Benzene.Avro;

/// <summary>Resolves (and caches) the Avro <see cref="Schema"/> to use for a CLR type.</summary>
public interface IAvroSchemaResolver
{
    /// <summary>Gets the Avro schema for <paramref name="type"/>.</summary>
    /// <param name="type">The CLR type to resolve a schema for.</param>
    /// <returns>The parsed Avro schema.</returns>
    Schema GetSchema(Type type);
}

/// <summary>
/// Default <see cref="IAvroSchemaResolver"/>: returns an explicitly-registered schema when one exists
/// for the type, otherwise falls back to a reflection-generated schema (unless
/// <see cref="AvroOptions.UseReflectionSchemas"/> is disabled, in which case an unregistered type
/// throws). Parsed schemas are cached per type.
/// </summary>
public class AvroSchemaResolver : IAvroSchemaResolver
{
    private readonly AvroOptions _options;
    private readonly ConcurrentDictionary<Type, Schema> _cache = new();

    /// <summary>Initializes a new instance of the <see cref="AvroSchemaResolver"/> class.</summary>
    /// <param name="options">The configured Avro options.</param>
    public AvroSchemaResolver(AvroOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public Schema GetSchema(Type type)
    {
        return _cache.GetOrAdd(type, Resolve);
    }

    private Schema Resolve(Type type)
    {
        if (_options.ExplicitSchemas.TryGetValue(type, out var explicitSchema))
        {
            return Schema.Parse(explicitSchema);
        }

        if (!_options.UseReflectionSchemas)
        {
            throw new InvalidOperationException(
                $"No Avro schema is registered for '{type.FullName}' and reflection schema generation is disabled. " +
                $"Register one via AddAvro(o => o.RegisterSchema<{type.Name}>(\"...\")) or enable UseReflectionSchemas.");
        }

        return Schema.Parse(AvroSchemaGenerator.Generate(type));
    }
}
