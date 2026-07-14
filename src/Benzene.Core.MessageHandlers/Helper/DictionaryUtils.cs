using System.Linq.Expressions;

namespace Benzene.Core.MessageHandlers.Helper;

/// <summary>
/// Dictionary merge/overlay/reflection helpers used mainly by <see cref="Benzene.Core.MessageHandlers.Request.EnrichingRequestMapper{TContext}"/>
/// to fold enricher-supplied values onto an already-mapped request.
/// </summary>
public static class DictionaryUtils
{
    /// <summary>
    /// Overlays string-valued entries onto <paramref name="source"/>, only filling in keys that are
    /// missing or currently default (see the object-dictionary overload of <c>MapOnto</c> below).
    /// Keys are matched and stored in lower-invariant form.
    /// </summary>
    /// <param name="source">The dictionary to overlay onto (mutated in place).</param>
    /// <param name="overlayDictionary">The values to overlay, or <c>null</c> to do nothing.</param>
    public static void MapOnto(IDictionary<string, object> source, IDictionary<string, string> overlayDictionary)
    {
        MapOnto(source, overlayDictionary?.ToDictionary(x => x.Key, x => x.Value as object));
    }

    /// <summary>
    /// Overlays entries from <paramref name="overlayDictionary"/> onto <paramref name="source"/>, only
    /// filling in keys that are missing from <paramref name="source"/> or whose existing value is
    /// <c>default</c>; existing non-default values are left untouched. Keys are matched and stored in
    /// lower-invariant form.
    /// </summary>
    /// <param name="source">The dictionary to overlay onto (mutated in place, and returned).</param>
    /// <param name="overlayDictionary">The values to overlay, or <c>null</c> to do nothing.</param>
    /// <returns><paramref name="source"/>, for chaining.</returns>
    public static IDictionary<string, object> MapOnto(IDictionary<string, object> source, IDictionary<string, object> overlayDictionary)
    {
        if (overlayDictionary == null)
        {
            return source;
        }

        foreach (var overlay in overlayDictionary)
        {
            var key = overlay.Key.ToLowerInvariant();

            if (!source.TryGetValue(key, out var existingValue))
            {
                source.Add(key, overlay.Value);
            }
            else if (existingValue == default)
            {
                source[key] = overlay.Value;
            }
        }

        return source;
    }

    /// <summary>
    /// Merges several dictionaries into one, keeping the first value seen for any duplicate key
    /// (dictionaries earlier in <paramref name="source"/> take precedence).
    /// </summary>
    /// <typeparam name="TKey">The dictionaries' key type.</typeparam>
    /// <typeparam name="TValue">The dictionaries' value type.</typeparam>
    /// <param name="source">The dictionaries to combine, in precedence order.</param>
    /// <returns>A new dictionary containing the combined entries.</returns>
    public static IDictionary<TKey, TValue> Combine<TKey, TValue>(IEnumerable<IDictionary<TKey, TValue>> source)
    {
        var output = new Dictionary<TKey, TValue>();

        foreach (var dictionary in source)
        {
            foreach (var keyValue in dictionary)
            {
                output.TryAdd(keyValue.Key, keyValue.Value);
            }
        }

        return output;
    }

    public static IDictionary<string, string> FilterAndReplace(IDictionary<string, string> source,
        IDictionary<string, string> filter)
    {
        return source
            .Where(x => filter.ContainsKey(x.Key.ToLowerInvariant()))
            .Select(x => (filter[x.Key.ToLowerInvariant()], source[x.Key]))
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }

    /// <summary>
    /// Renames <paramref name="source"/>'s entries using <paramref name="filter"/> as a key-rename map,
    /// keeping entries whose key is not in <paramref name="filter"/> unchanged (unlike
    /// <see cref="FilterAndReplace"/>, which drops them).
    /// </summary>
    /// <param name="source">The dictionary to rename entries from.</param>
    /// <param name="filter">Maps source keys (case-insensitive) to the output key to rename them to.</param>
    /// <returns>A new dictionary with matched keys renamed and unmatched keys kept as-is.</returns>
    public static IDictionary<string, string> Replace(IDictionary<string, string> source,
        IDictionary<string, string> filter)
    {
        return source
            .Select(x => filter.ContainsKey(x.Key.ToLowerInvariant())
                ? (filter[x.Key.ToLowerInvariant()], source[x.Key])
                : (x.Key, x.Value)
            )
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }

    /// <summary>
    /// Sets a value on a dictionary by key (overwriting any existing value).
    /// </summary>
    /// <param name="dictionary">The dictionary to set the value on.</param>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to set.</param>
    public static void Set(IDictionary<string, string> dictionary, string key, string value)
    {
        dictionary[key] = value;
    }

    /// <summary>
    /// Checks whether a dictionary contains the given key with exactly the given value.
    /// </summary>
    /// <param name="dictionary">The dictionary to check, or <c>null</c> (in which case <c>false</c> is returned).</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns><c>true</c> if the key exists and its value equals <paramref name="value"/>; otherwise <c>false</c>.</returns>
    public static bool KeyEquals(IDictionary<string, string> dictionary, string key, string value)
    {
        if (dictionary != null && dictionary.TryGetValue(key, out var keyValue))
        {
            return keyValue == value;
        }

        return false;
    }

    /// <summary>
    /// Sets each property on <paramref name="source"/> whose name matches (case-insensitively) a key
    /// in <paramref name="dictionary"/> to that entry's value, converting the value's type if needed.
    /// Used by <see cref="Benzene.Core.MessageHandlers.Request.EnrichingRequestMapper{TContext}"/> to fold enricher-supplied values
    /// onto a mapped request object.
    /// </summary>
    /// <typeparam name="T">The type of the object being enriched.</typeparam>
    /// <param name="source">The object to enrich, or <c>null</c> (in which case a new instance is created via <see cref="Activator.CreateInstance{T}()"/> before enriching).</param>
    /// <param name="dictionary">The property values to apply, keyed by property name.</param>
    /// <returns>The enriched object (a new instance if <paramref name="source"/> was <c>null</c>).</returns>
    public static T Enrich<T>(T source, IDictionary<string, object> dictionary)
    {
        if (dictionary.Count == 0)
        {
            return source;
        }

        var setters = PropertySetterCache<T>.Setters;
        if (setters.Count == 0)
        {
            return source;
        }

        // Build one case-insensitive lookup over the caller's dictionary up front (O(dictionary size)),
        // instead of the previous per-property linear .Where() scan (O(properties x dictionary size)).
        // TryAdd (not the case-insensitive-dictionary copy constructor) so two differently-cased keys
        // in the source don't throw - the first one wins, matching the original .First() semantics.
        var caseInsensitiveValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in dictionary)
        {
            caseInsensitiveValues.TryAdd(entry.Key, entry.Value);
        }

        foreach (var (propertyName, setter) in setters)
        {
            if (caseInsensitiveValues.TryGetValue(propertyName, out var value))
            {
                source = EnsureNotNull(source);
                setter.Set(source, GetValue(value, setter.PropertyType));
            }
        }

        return source;
    }

    private static T EnsureNotNull<T>(T source)
    {
        return source ?? Activator.CreateInstance<T>();
    }

    private static object GetValue(object originalValue, Type propertyType)
    {
        if (originalValue.GetType() != propertyType)
        {
            return Convert.ChangeType(originalValue, propertyType);
        }

        return originalValue;
    }

    /// <summary>
    /// Caches a compiled setter delegate per writable public instance property of <typeparamref name="T"/>,
    /// built once (via <see cref="System.Linq.Expressions"/>) the first time <typeparamref name="T"/> is
    /// enriched, so repeated <see cref="Enrich{T}"/> calls for the same request type avoid re-reflecting
    /// over <see cref="Type.GetProperties()"/> and calling <c>PropertyInfo.SetValue</c> per property, per
    /// message. Non-writable properties are silently excluded (rather than replicating the reflective
    /// path's <c>ArgumentException</c> if such a property happened to match a dictionary key - an
    /// unencountered edge case in practice, since enrichment targets are always plain settable DTOs).
    /// </summary>
    private static class PropertySetterCache<T>
    {
        public static readonly IReadOnlyDictionary<string, (Type PropertyType, Action<T, object> Set)> Setters = Build();

        private static Dictionary<string, (Type, Action<T, object>)> Build()
        {
            var setters = new Dictionary<string, (Type, Action<T, object>)>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in typeof(T).GetProperties())
            {
                var setMethod = property.GetSetMethod();
                if (setMethod == null)
                {
                    continue;
                }

                var instanceParameter = Expression.Parameter(typeof(T), "instance");
                var valueParameter = Expression.Parameter(typeof(object), "value");
                var convertedValue = Expression.Convert(valueParameter, property.PropertyType);
                var call = Expression.Call(instanceParameter, setMethod, convertedValue);
                var compiled = Expression.Lambda<Action<T, object>>(call, instanceParameter, valueParameter).Compile();

                setters[property.Name] = (property.PropertyType, compiled);
            }

            return setters;
        }
    }
}
