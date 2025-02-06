using Benzene.Abstractions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Correlation;
using Benzene.Core.Middleware;

namespace Benzene.Diagnostics.Correlation;

public static class Extensions
{
    public static IBenzeneServiceContainer AddCorrelationId(this IBenzeneServiceContainer services)
    {
        return services.AddScoped<ICorrelationId, CorrelationId>();
    }

    public static IMiddlewarePipelineBuilder<TContext> UseCorrelationId<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string header = "correlationId")
    {
        app.Register(x => x.AddCorrelationId());

        app.Use(resolver => new FuncWrapperMiddleware<TContext>("CorrelationId", async (context, next) =>
        {
            var setCorrelationId = resolver.GetService<ICorrelationId>();
            var messageMapper = resolver.GetService<IMessageHeadersGetter<TContext>>();
            var correlationId = messageMapper.GetHeader(context, header);
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

    public static string GetHeader<TContext>(this IMessageHeadersGetter<TContext> source, TContext context, string key)
    {
        var headers = source.GetHeaders(context);

        return !headers.TryGetValue(key, out var header) ? string.Empty : header;
    }

}