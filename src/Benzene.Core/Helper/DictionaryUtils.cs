using System;
using System.Collections.Generic;
using System.Linq;

namespace Benzene.Core.Helper;

/// <summary>
/// Provides utility methods for manipulating and combining dictionaries.
/// </summary>
public static class DictionaryUtils
{
    /// <summary>
    /// Maps values from an overlay dictionary onto a source dictionary using case-insensitive keys.
    /// </summary>
    /// <param name="source">The source dictionary to modify.</param>
    /// <param name="overlayDictionary">The overlay dictionary containing values to map.</param>
    public static void MapOnto(IDictionary<string, object> source, IDictionary<string, string> overlayDictionary)
    {
        MapOnto(source, overlayDictionary?.ToDictionary(x => x.Key, x => x.Value as object));
    }

    /// <summary>
    /// Maps values from an overlay dictionary onto a source dictionary using case-insensitive keys.
    /// Only adds or updates keys that are missing or have default values in the source.
    /// </summary>
    /// <param name="source">The source dictionary to modify.</param>
    /// <param name="overlayDictionary">The overlay dictionary containing values to map.</param>
    /// <returns>The modified source dictionary.</returns>
    public static IDictionary<string, object> MapOnto(IDictionary<string, object> source, IDictionary<string, object> overlayDictionary)
    {
        if (overlayDictionary == null)
        {
            return source;
        }

        foreach (var overlay in overlayDictionary)
        {
            if (!source.ContainsKey(overlay.Key.ToLowerInvariant()))
            {
                source.Add(overlay.Key.ToLowerInvariant(), overlay.Value);
            }
            else if (source[overlay.Key.ToLowerInvariant()] == default)
            {
                source[overlay.Key.ToLowerInvariant()] = overlay.Value;
            }
        }

        return source;
    }
    
    /// <summary>
    /// Combines multiple dictionaries into a single dictionary, using the first occurrence of each key.
    /// </summary>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of dictionary values.</typeparam>
    /// <param name="source">The collection of dictionaries to combine.</param>
    /// <returns>A combined dictionary containing all unique keys from the source dictionaries.</returns>
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

    // public static IDictionary<TKey, TValue> Map<TKey, TValue>(IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> overlayDictionary)
    //     where TValue : class where TKey : notnull
    // {
    //     if (source == null)
    //     {
    //         return null;
    //     }
    //
    //
    //     var output = source.ToDictionary(x => x.Key, x => x.Value);
    //
    //     if (overlayDictionary == null)
    //     {
    //         return output;
    //     }
    //
    //     foreach (var overlay in overlayDictionary)
    //     {
    //         if (!output.ContainsKey(overlay.Key))
    //         {
    //             output.Add(overlay.Key, overlay.Value);
    //         }
    //         else if (source[overlay.Key] == default(TValue))
    //         {
    //             output[overlay.Key] = overlay.Value;
    //         }
    //     }
    //
    //     return output;
    // }

    // public static Dictionary<string, object> JsonToDictionary(string json)
    // {
    //     if (string.IsNullOrEmpty(json))
    //     {
    //         return new Dictionary<string, object>();
    //     }
    //
    //     var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    //     return dictionary?.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value);
    // }

    /// <summary>
    /// Filters and replaces keys in a source dictionary based on a filter dictionary using case-insensitive matching.
    /// </summary>
    /// <param name="source">The source dictionary to filter.</param>
    /// <param name="filter">The filter dictionary mapping old keys to new keys.</param>
    /// <returns>A new dictionary with filtered and replaced keys.</returns>
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
    /// Replaces keys in a source dictionary based on a filter dictionary using case-insensitive matching.
    /// </summary>
    /// <param name="source">The source dictionary to process.</param>
    /// <param name="filter">The filter dictionary mapping old keys to new keys.</param>
    /// <returns>A new dictionary with replaced keys.</returns>
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
    /// Sets a value in the dictionary for the specified key.
    /// </summary>
    /// <param name="dictionary">The dictionary to modify.</param>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to set.</param>
    public static void Set(IDictionary<string, string> dictionary, string key, string value)
    {
        dictionary[key] = value;
    }

    /// <summary>
    /// Checks if a dictionary key equals a specific value.
    /// </summary>
    /// <param name="dictionary">The dictionary to check.</param>
    /// <param name="key">The key to check.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>True if the key exists and its value equals the specified value; otherwise, false.</returns>
    public static bool KeyEquals(IDictionary<string, string> dictionary, string key, string value)
    {
        if (dictionary != null && dictionary.TryGetValue(key, out var keyValue))
        {
            return keyValue == value;
        }
    
        return false;
    }

    /// <summary>
    /// Enriches an object by setting its properties from a dictionary using case-insensitive property name matching.
    /// </summary>
    /// <typeparam name="T">The type of object to enrich.</typeparam>
    /// <param name="source">The object to enrich.</param>
    /// <param name="dictionary">The dictionary containing property values.</param>
    /// <returns>The enriched object.</returns>
    public static T Enrich<T>(T source, IDictionary<string, object> dictionary)
    {
        foreach (var propertyInfo in typeof(T).GetProperties())
        {
            var fields = dictionary.Where(x =>
                x.Key.Equals(propertyInfo.Name, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            if (fields.Any())
            {
                source = EnsureNotNull(source);
                propertyInfo.SetValue(source, fields.First().Value);
            }
        }

        return source;
    }

    private static T EnsureNotNull<T>(T source)
    {
        return source ?? Activator.CreateInstance<T>();
    }

    // public class RootWrapper<T>
    // {
    //     public T Root { get; set; }
    // }
}
