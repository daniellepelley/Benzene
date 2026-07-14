using System;
using Benzene.Abstractions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Diagnostics.Correlation;

public static class Extensions
{
    public static IBenzeneServiceContainer AddCorrelationId(this IBenzeneServiceContainer services)
    {
        return services.AddScoped<ICorrelationId, CorrelationId>();
    }

    public static ILogContextBuilder<TContext> WithCorrelationId<TContext>(this ILogContextBuilder<TContext> source)
    {
        source.Register(x => x.AddCorrelationId());
        return source.OnRequest("correlationId", resolver =>
        {
            var correlationId = resolver.GetService<ICorrelationId>();
            return correlationId.Get();
        });
    }

    /// <summary>
    /// Looks up a single header, matching the key case-insensitively.
    /// </summary>
    public static string GetHeader<TContext>(this IMessageHeadersGetter<TContext> source, TContext context, string key)
        => GetHeader(source, context, new[] { key });

    /// <summary>
    /// Looks up the first of several candidate header keys that is present (matched case-insensitively),
    /// in the order given.
    /// </summary>
    public static string GetHeader<TContext>(this IMessageHeadersGetter<TContext> source, TContext context, IReadOnlyList<string> keys)
    {
        var headers = source.GetHeaders(context);
        foreach (var key in keys)
        {
            foreach (var pair in headers)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(pair.Value))
                {
                    return pair.Value;
                }
            }
        }

        return string.Empty;
    }
}