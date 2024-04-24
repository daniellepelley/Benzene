using System.Collections.Specialized;

namespace Benzene.SelfHost.Http;

internal static class InternalExtensions
{
    public static IDictionary<string, string> ToDictionary(this NameValueCollection nameValueCollection)
    {
        IDictionary<string, string> dictionary = new Dictionary<string, string>();
        foreach (string key in nameValueCollection.AllKeys)
        {
            dictionary.Add(key.ToLowerInvariant(), nameValueCollection[key].ToLowerInvariant());
        }

        return dictionary;
    }

}