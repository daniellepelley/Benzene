namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// Startup helper that registers message-type schemas with an <see cref="ISchemaRegistryClient"/> and
/// builds the per-type id map a <see cref="SchemaRegistrySerializer"/> needs, plus a fail-fast
/// evolution check. Run this once at startup (before wiring the pipeline), so registration and
/// compatibility happen up front — not on the first message.
/// </summary>
public class SchemaRegistrar
{
    private readonly ISchemaRegistryClient _registry;
    private readonly ISchemaResolver _resolver;

    /// <summary>Initializes the registrar.</summary>
    /// <param name="registry">The registry to register/check against.</param>
    /// <param name="resolver">Resolves each type's schema definition.</param>
    public SchemaRegistrar(ISchemaRegistryClient registry, ISchemaResolver resolver)
    {
        _registry = registry;
        _resolver = resolver;
    }

    /// <summary>Registers each type's schema and returns the resulting per-type schema-id map.</summary>
    /// <param name="types">The message types to register.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<IReadOnlyDictionary<Type, int>> RegisterAsync(
        IEnumerable<Type> types, CancellationToken cancellationToken = default)
    {
        var map = new Dictionary<Type, int>();
        foreach (var type in types)
        {
            map[type] = await _registry.RegisterAsync(_resolver.Resolve(type), cancellationToken);
        }

        return map;
    }

    /// <summary>
    /// Verifies every type's current schema is compatible with the registry, throwing
    /// <see cref="SchemaIncompatibleException"/> listing all incompatible subjects at once — an
    /// evolution gate complementing the A.2 contract compatibility check.
    /// </summary>
    /// <param name="types">The message types to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task EnsureCompatibleAsync(IEnumerable<Type> types, CancellationToken cancellationToken = default)
    {
        var incompatible = new List<string>();
        foreach (var type in types)
        {
            var definition = _resolver.Resolve(type);
            if (!await _registry.IsCompatibleAsync(definition, cancellationToken))
            {
                incompatible.Add(definition.Subject);
            }
        }

        if (incompatible.Count > 0)
        {
            throw new SchemaIncompatibleException(string.Join(", ", incompatible));
        }
    }

    /// <summary>
    /// Registers the given types' schemas and returns a <see cref="SchemaRegistrySerializer"/> that
    /// frames <paramref name="inner"/>'s output with the resolved ids.
    /// </summary>
    /// <param name="inner">The serializer whose output is framed.</param>
    /// <param name="types">The message types to register.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<SchemaRegistrySerializer> CreateSerializerAsync(
        Benzene.Abstractions.Serialization.IPayloadSerializer inner,
        IEnumerable<Type> types,
        CancellationToken cancellationToken = default)
    {
        var map = await RegisterAsync(types, cancellationToken);
        return new SchemaRegistrySerializer(inner, map);
    }
}
