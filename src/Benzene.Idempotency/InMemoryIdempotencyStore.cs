namespace Benzene.Idempotency;

/// <summary>
/// An in-process <see cref="IIdempotencyStore"/> backed by a dictionary, suitable for a single
/// worker instance, tests, and local development.
/// </summary>
/// <remarks>
/// State lives in this process only. In a multi-instance deployment each instance keeps its own map,
/// so a duplicate redelivered to a different instance is NOT de-duplicated — use a shared store
/// (e.g. Redis) there. Records are held for a configurable time-to-live and expired lazily on the
/// next access to a key.
/// </remarks>
public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private sealed class Entry
    {
        public IdempotencyStatus Status { get; init; }
        public bool WasSuccessful { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
    }

    private readonly Dictionary<string, Entry> _entries = new();
    private readonly object _gate = new();
    private readonly TimeSpan _timeToLive;
    private readonly Func<DateTimeOffset> _now;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryIdempotencyStore"/> class.
    /// </summary>
    /// <param name="timeToLive">How long a record is retained. Defaults to 24 hours.</param>
    /// <param name="now">
    /// A clock, overridable for tests. Defaults to <see cref="DateTimeOffset.UtcNow"/>.
    /// </param>
    public InMemoryIdempotencyStore(TimeSpan? timeToLive = null, Func<DateTimeOffset>? now = null)
    {
        _timeToLive = timeToLive ?? TimeSpan.FromHours(24);
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public Task<ClaimResult> TryClaimAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var now = _now();
            if (_entries.TryGetValue(key, out var existing) && existing.ExpiresAt > now)
            {
                var record = new IdempotencyRecord(key, existing.Status, existing.WasSuccessful);
                return Task.FromResult(ClaimResult.AlreadyExists(record));
            }

            _entries[key] = new Entry
            {
                Status = IdempotencyStatus.InProgress,
                ExpiresAt = now + _timeToLive
            };
            return Task.FromResult(ClaimResult.Won());
        }
    }

    /// <inheritdoc />
    public Task CompleteAsync(string key, bool wasSuccessful, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _entries[key] = new Entry
            {
                Status = IdempotencyStatus.Completed,
                WasSuccessful = wasSuccessful,
                ExpiresAt = _now() + _timeToLive
            };
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _entries.Remove(key);
        }
        return Task.CompletedTask;
    }
}
