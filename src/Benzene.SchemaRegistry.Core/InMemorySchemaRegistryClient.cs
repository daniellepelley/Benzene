namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// An in-process <see cref="ISchemaRegistryClient"/> — for tests, local development, and single-node
/// deployments that want registry-framed payloads without running a registry server. Assigns
/// monotonic ids, versions per subject, dedups an identical re-registration, and enforces
/// compatibility via a pluggable <see cref="ISchemaCompatibilityChecker"/>.
/// </summary>
/// <remarks>
/// State lives in this process only — it does not coordinate ids across instances, so two instances
/// can assign different ids to the same schema. Use a shared registry (Confluent/Azure) across
/// instances; this is the seam's reference implementation and test double.
/// </remarks>
public class InMemorySchemaRegistryClient : ISchemaRegistryClient
{
    private readonly object _gate = new();
    private readonly List<RegisteredSchema> _byId = new();
    private readonly Dictionary<string, List<RegisteredSchema>> _bySubject = new();
    private readonly ISchemaCompatibilityChecker _checker;
    private readonly SchemaCompatibilityMode _mode;
    private int _nextId = 1;

    /// <summary>Initializes the registry.</summary>
    /// <param name="mode">The compatibility level enforced on registration. Defaults to <see cref="SchemaCompatibilityMode.Backward"/>.</param>
    /// <param name="checker">The compatibility checker. Defaults to <see cref="TextualSchemaCompatibilityChecker"/>.</param>
    public InMemorySchemaRegistryClient(
        SchemaCompatibilityMode mode = SchemaCompatibilityMode.Backward,
        ISchemaCompatibilityChecker? checker = null)
    {
        _mode = mode;
        _checker = checker ?? new TextualSchemaCompatibilityChecker();
    }

    /// <inheritdoc />
    public Task<int> RegisterAsync(SchemaDefinition schema, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var versions = Versions(schema.Subject);

            var existing = versions.FirstOrDefault(v =>
                string.Equals(v.Schema, schema.Schema, StringComparison.Ordinal) && v.Format == schema.Format);
            if (existing != null)
            {
                return Task.FromResult(existing.Id); // idempotent
            }

            var latest = versions.Count > 0 ? versions[^1] : null;
            if (!_checker.IsCompatible(latest, schema, _mode))
            {
                throw new SchemaIncompatibleException(schema.Subject);
            }

            var registered = new RegisteredSchema(_nextId++, schema.Subject, versions.Count + 1, schema.Schema, schema.Format);
            versions.Add(registered);
            _byId.Add(registered);
            return Task.FromResult(registered.Id);
        }
    }

    /// <inheritdoc />
    public Task<RegisteredSchema?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_byId.FirstOrDefault(s => s.Id == id));
        }
    }

    /// <inheritdoc />
    public Task<RegisteredSchema?> GetLatestAsync(string subject, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var versions = Versions(subject);
            return Task.FromResult(versions.Count > 0 ? versions[^1] : null);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsCompatibleAsync(SchemaDefinition schema, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var versions = Versions(schema.Subject);
            var latest = versions.Count > 0 ? versions[^1] : null;
            return Task.FromResult(_checker.IsCompatible(latest, schema, _mode));
        }
    }

    private List<RegisteredSchema> Versions(string subject)
    {
        if (!_bySubject.TryGetValue(subject, out var versions))
        {
            versions = new List<RegisteredSchema>();
            _bySubject[subject] = versions;
        }

        return versions;
    }
}
