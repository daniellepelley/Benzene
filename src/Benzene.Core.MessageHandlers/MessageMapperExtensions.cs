using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Convenience extension methods for reading a single header value out of an <see cref="IMessageHeadersGetter{TContext}"/>.
/// </summary>
public static class MessageMapperExtensions
{
    /// <summary>
    /// Gets a single header value by key from the message's headers.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="source">The headers getter to read from.</param>
    /// <param name="context">The context to extract headers from.</param>
    /// <param name="key">The header name to look up.</param>
    /// <param name="ignoreCase">
    /// Whether the lookup should be case-insensitive (the default). When <c>true</c>, if multiple
    /// headers differ only by case, the last one encountered wins.
    /// </param>
    /// <returns>The header value, or <c>null</c> if no header with that key exists.</returns>
    public static string GetHeader<TContext>(this IMessageHeadersGetter<TContext> source, TContext context, string key, bool ignoreCase = true)
    {
        var headers = GetHeaders(source, context, ignoreCase);

        if (!headers.ContainsKey(key))
        {
            return null;
        }

        return headers[key];
    }

    private static IDictionary<string, string> GetHeaders<TContext>(IMessageHeadersGetter<TContext> source,
        TContext context,
        bool ignoreCase)
    {
        var headers = source.GetHeaders(context);

        if (!ignoreCase)
        {
            return headers;
        }

        var output = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var header in headers)
        {
            output[header.Key] = header.Value;
        }
        return output;
    }
}
