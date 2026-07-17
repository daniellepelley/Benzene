using System.Collections.Concurrent;

namespace Benzene.Configuration.Core;

/// <summary>
/// Caches values from an inner store for a time-to-live, so a remote secret store (Key Vault, AWS
/// Secrets Manager, ...) is not hit on every read. This is the "optional reload" seam: a cached
/// value refreshes when its TTL lapses, and <see cref="Invalidate"/>/<see cref="InvalidateAll"/>
/// force an immediate re-fetch — e.g. after a secret rotation.
/// </summary>
/// <remarks>
/// A <c>null</c> (missing) result is cached too, so a genuinely-absent name is not re-queried on
/// every read within the TTL. Concurrent misses for the same name may each fetch once — harmless, so
/// no lock is taken on the read path.
/// </remarks>
public class CachingSecretStore : ISecretStore
{
    private sealed class Entry
    {
        public string? Value { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
    }

    private readonly ISecretStore _inner;
    private readonly TimeSpan _timeToLive;
    private readonly Func<DateTimeOffset> _now;
    private readonly ConcurrentDictionary<string, Entry> _cache = new();

    /// <summary>Initializes the cache.</summary>
    /// <param name="inner">The store whose values are cached.</param>
    /// <param name="timeToLive">How long a value is cached. Defaults to 5 minutes.</param>
    /// <param name="now">A clock, overridable for tests. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    public CachingSecretStore(ISecretStore inner, TimeSpan? timeToLive = null, Func<DateTimeOffset>? now = null)
    {
        _inner = inner;
        _timeToLive = timeToLive ?? TimeSpan.FromMinutes(5);
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var now = _now();
        if (_cache.TryGetValue(name, out var entry) && entry.ExpiresAt > now)
        {
            return entry.Value;
        }

        var value = await _inner.GetSecretAsync(name, cancellationToken);
        _cache[name] = new Entry { Value = value, ExpiresAt = now + _timeToLive };
        return value;
    }

    /// <summary>Drops the cached value for <paramref name="name"/> so the next read re-fetches it.</summary>
    public void Invalidate(string name) => _cache.TryRemove(name, out _);

    /// <summary>Drops every cached value so the next read of each re-fetches it.</summary>
    public void InvalidateAll() => _cache.Clear();
}
