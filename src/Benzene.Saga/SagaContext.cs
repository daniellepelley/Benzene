namespace Benzene.Saga;

/// <summary>
/// Carries the results of completed saga steps so that a later stage can read what an earlier stage
/// produced (e.g. a stage-2 step using the tenant ID a stage-1 step created). A succeeded step
/// publishes its result here after its stage completes; keys are the result's type by default, or an
/// explicit string key when a stage produces more than one value of the same type.
/// </summary>
/// <remarks>
/// Steps within a single stage run concurrently but only ever <em>read</em> earlier stages' values
/// during that concurrent phase; writes happen single-threaded after each stage's barrier, so no
/// synchronization is required here.
/// </remarks>
public class SagaContext
{
    private readonly Dictionary<string, object?> _items = new();

    private static string KeyFor<T>(string? key) => key ?? typeof(T).FullName ?? typeof(T).Name;

    /// <summary>Stores <paramref name="value"/>, keyed by <paramref name="key"/> or by <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="value">The value to store.</param>
    /// <param name="key">An optional explicit key; defaults to <typeparamref name="T"/>'s name.</param>
    public void Set<T>(T value, string? key = null) => _items[KeyFor<T>(key)] = value;

    /// <summary>Gets a previously stored value.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="key">The optional explicit key it was stored under.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="KeyNotFoundException">No value is stored for the type/key.</exception>
    public T Get<T>(string? key = null)
    {
        if (!_items.TryGetValue(KeyFor<T>(key), out var value))
        {
            throw new KeyNotFoundException($"No saga context value for '{KeyFor<T>(key)}'.");
        }

        return (T)value!;
    }

    /// <summary>Attempts to get a previously stored value.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="value">The stored value, if present.</param>
    /// <param name="key">The optional explicit key it was stored under.</param>
    /// <returns><c>true</c> if a value was found; otherwise <c>false</c>.</returns>
    public bool TryGet<T>(out T value, string? key = null)
    {
        if (_items.TryGetValue(KeyFor<T>(key), out var stored) && stored is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Returns whether a value is stored for the given type/key.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="key">The optional explicit key.</param>
    /// <returns><c>true</c> if present; otherwise <c>false</c>.</returns>
    public bool Has<T>(string? key = null) => _items.ContainsKey(KeyFor<T>(key));
}
