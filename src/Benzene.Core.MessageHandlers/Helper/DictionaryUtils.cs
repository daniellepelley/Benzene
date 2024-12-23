namespace Benzene.Core.MessageHandlers.Helper;

public static class DictionaryUtils
{
    public static void MapOnto(IDictionary<string, object> source, IDictionary<string, string> overlayDictionary)
    {
        MapOnto(source, overlayDictionary?.ToDictionary(x => x.Key, x => x.Value as object));
    }

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

    public static void Set(IDictionary<string, string> dictionary, string key, string value)
    {
        dictionary[key] = value;
    }

    public static bool KeyEquals(IDictionary<string, string> dictionary, string key, string value)
    {
        if (dictionary != null && dictionary.TryGetValue(key, out var keyValue))
        {
            return keyValue == value;
        }
    
        return false;
    }

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
