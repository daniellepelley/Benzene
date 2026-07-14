namespace Benzene.Core.Messages.Helper;

/// <summary>
/// Compares media-type header values (e.g. a <c>content-type</c> or <c>accept</c> header) against a
/// target media type, tolerant of the parameters and casing real HTTP traffic carries — unlike a
/// plain string-equality check, <c>"application/json; charset=utf-8"</c> and <c>"Application/JSON"</c>
/// both match <c>"application/json"</c>.
/// </summary>
public static class MediaType
{
    /// <summary>
    /// Checks whether a media-type header value matches a target media type, ignoring any
    /// <c>;</c>-delimited parameters (e.g. <c>charset</c>) and comparing case-insensitively.
    /// </summary>
    /// <param name="headerValue">The raw header value to check, e.g. <c>"application/json; charset=utf-8"</c>.</param>
    /// <param name="mediaType">The target media type to match against, e.g. <c>"application/json"</c>.</param>
    /// <returns><c>true</c> if the header value's media type (ignoring parameters) equals <paramref name="mediaType"/>; otherwise <c>false</c>.</returns>
    public static bool Matches(string? headerValue, string mediaType)
    {
        if (string.IsNullOrEmpty(headerValue))
        {
            return false;
        }

        var semicolonIndex = headerValue.IndexOf(';');
        var value = semicolonIndex >= 0 ? headerValue[..semicolonIndex] : headerValue;

        return string.Equals(value.Trim(), mediaType.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
