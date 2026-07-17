namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// The neutral schema-registry seam: register a schema under a subject and get back a stable id,
/// look a schema up by id or subject, and check whether a candidate schema is compatible. An app
/// depends on this; a provider adapter (Confluent Schema Registry, Azure Schema Registry, ...)
/// implements it, so registry-backed serialization and evolution checks aren't tied to one vendor.
/// </summary>
public interface ISchemaRegistryClient
{
    /// <summary>
    /// Registers <paramref name="schema"/> under its subject and returns its registry-wide id.
    /// Registering an identical schema again returns the existing id (idempotent).
    /// </summary>
    /// <param name="schema">The schema to register.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<int> RegisterAsync(SchemaDefinition schema, CancellationToken cancellationToken = default);

    /// <summary>Returns the schema with the given id, or <c>null</c> if unknown.</summary>
    /// <param name="id">The registry-wide schema id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<RegisteredSchema?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns the latest registered version for <paramref name="subject"/>, or <c>null</c> if none.</summary>
    /// <param name="subject">The subject to look up.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<RegisteredSchema?> GetLatestAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether <paramref name="schema"/> is compatible with the subject's existing versions
    /// under the registry's configured compatibility mode (a first schema for a subject is always
    /// compatible). Use this as an evolution check before publishing a new contract.
    /// </summary>
    /// <param name="schema">The candidate schema.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<bool> IsCompatibleAsync(SchemaDefinition schema, CancellationToken cancellationToken = default);
}
