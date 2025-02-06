namespace Benzene.Core.Messages.Helper;

public static class DictionaryUtils
{
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
}
