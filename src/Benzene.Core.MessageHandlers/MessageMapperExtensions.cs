using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Core.MessageHandlers;

public static class MessageMapperExtensions
{
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
