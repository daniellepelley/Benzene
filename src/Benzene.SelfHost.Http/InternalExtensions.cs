using System.Collections.Specialized;

namespace Benzene.SelfHost.Http;

internal static class InternalExtensions
{
    public static IDictionary<string, string> ToDictionary(this NameValueCollection nameValueCollection)
    {
        IDictionary<string, string> dictionary = new Dictionary<string, string>();
        foreach (string key in nameValueCollection.AllKeys)
        {
            // Header/query-string field NAMES are case-insensitive (lower-case the key for lookup
            // stability), but VALUES are opaque and case-sensitive (RFC 9110) - lower-casing a value
            // corrupts Authorization tokens, cookies, ETags, traceparent, signed URLs, etc. Use the
            // indexer, not Add, so two keys differing only by case (e.g. ?A=x&a=y) can't throw.
            dictionary[key.ToLowerInvariant()] = nameValueCollection[key];
        }

        return dictionary;
    }

}