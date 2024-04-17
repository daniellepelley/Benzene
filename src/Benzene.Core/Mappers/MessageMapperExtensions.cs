using System;
using System.Collections.Generic;
using Benzene.Abstractions.Mappers;

namespace Benzene.Core.Mappers;

public static class MessageMapperExtensions
{
    public static string GetHeader<TContext>(this IMessageHeadersMapper<TContext> source, TContext context, string key, bool ignoreCase = true)
    {
        var headers = GetHeaders(source, context, ignoreCase);

        if (!headers.ContainsKey(key))
        {
            return null;
        }

        return headers[key];
    }

    private static IDictionary<string, string> GetHeaders<TContext>(IMessageHeadersMapper<TContext> source,
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
