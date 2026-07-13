using System;
using Benzene.Abstractions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Diagnostics.Correlation;

public static class Extensions
{
    /// <summary>
    /// The header keys checked, in order, when <see cref="UseCorrelationId{TContext}"/> is called without
    /// an explicit header name. <c>correlationId</c> is kept last as a legacy fallback for callers already
    /// relying on the pre-fix default.
    /// </summary>
    private static readonly string[] DefaultHeaderKeys = { "x-correlation-id", "correlation-id", "correlationId" };

    public static IBenzeneServiceContainer AddCorrelationId(this IBenzeneServiceContainer services)
    {
        return services.AddScoped<ICorrelationId, CorrelationId>();
    }

    /// <summary>
    /// Adds middleware that picks up a correlation ID from the incoming message headers.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="header">A specific header key to check instead of the default fallback list
    /// (<c>x-correlation-id</c>, <c>correlation-id</c>, then the legacy <c>correlationId</c>).</param>
    /// <remarks>
    /// Superseded by automatic W3C <c>traceparent</c> propagation (see <c>AddDiagnostics()</c>'s
    /// <c>UseW3CTraceContext()</c>) for cross-service correlation. The <c>correlationId</c>-style header
    /// remains supported here as a legacy fallback and is still emitted to log scopes via
    /// <see cref="WithCorrelationId{TContext}"/>.
    /// </remarks>
    [Obsolete("Superseded by automatic W3C traceparent propagation (AddDiagnostics()'s UseW3CTraceContext()); " +
        "the correlationId-style header remains supported here as a legacy fallback.")]
    public static IMiddlewarePipelineBuilder<TContext> UseCorrelationId<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string? header = null)
    {
        app.Register(x => x.AddCorrelationId());

        app.Use(resolver => new FuncWrapperMiddleware<TContext>("CorrelationId", async (context, next) =>
        {
            var setCorrelationId = resolver.GetService<ICorrelationId>();
            var messageMapper = resolver.GetService<IMessageHeadersGetter<TContext>>();
            var correlationId = header is not null
                ? messageMapper.GetHeader(context, header)
                : messageMapper.GetHeader(context, DefaultHeaderKeys);
            if (!string.IsNullOrEmpty(correlationId))
            {
                setCorrelationId.Set(correlationId);
            }

            await next();
        }));
        return app;
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