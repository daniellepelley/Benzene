using System.Diagnostics;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Diagnostics;

/// <summary>
/// Provides a single, platform-agnostic middleware that enriches both log scopes and the current
/// <see cref="Activity"/> with the same set of keys, on every platform.
/// </summary>
public static class EnrichmentExtensions
{
    /// <summary>
    /// Adds middleware that attaches <c>invocationId</c>, <c>traceId</c>, <c>spanId</c>, <c>topic</c>,
    /// <c>transport</c>, and <c>handler</c> to the logging scope (via <see cref="ILogger.BeginScope"/>)
    /// for the duration of the request, and tags the current <see cref="Activity"/> with
    /// <c>benzene.invocationId</c>. Each key is resolved independently and simply omitted when its
    /// backing service (e.g. <see cref="IBenzeneInvocation"/>) isn't registered for this pipeline scope,
    /// so it's safe to add unconditionally on every platform and transport.
    /// </summary>
    /// <remarks>
    /// Replaces the AWS-only <c>WithRequestId()</c>/<c>WithApplication()</c>/<c>WithHttp()</c> log-context
    /// extensions with one portable call that works the same way on AWS Lambda, Azure Functions, and
    /// ASP.NET Core. <see cref="IBenzeneInvocation"/> is only populated where the hosting platform's
    /// <c>UseBenzeneInvocation()</c> has been called on this pipeline (or an outer one) — it does not
    /// automatically flow into a nested sub-application that creates its own DI scope (e.g. SQS/SNS/Kafka
    /// per-message dispatch); <c>invocationId</c> is simply omitted there today.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneEnrichment<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.Use("BenzeneEnrichment", resolver => async (context, next) =>
        {
            var activity = Activity.Current;
            var invocation = resolver.TryGetService<IBenzeneInvocation>();
            var transport = resolver.TryGetService<ICurrentTransport>();
            var topic = resolver.TryGetService<IMessageGetter<TContext>>()?.GetTopic(context);
            var handler = topic is not null && !string.IsNullOrEmpty(topic.Id)
                ? resolver.TryGetService<IMessageHandlerDefinitionLookUp>()?.FindHandler(topic)
                : null;

            var scope = new Dictionary<string, object>();
            if (invocation is not null)
            {
                scope["invocationId"] = invocation.InvocationId;
            }

            if (activity is not null)
            {
                scope["traceId"] = activity.TraceId.ToHexString();
                scope["spanId"] = activity.SpanId.ToHexString();
                if (invocation is not null)
                {
                    activity.SetTag("benzene.invocationId", invocation.InvocationId);
                }
            }

            if (transport is not null)
            {
                scope["transport"] = transport.Name;
            }

            if (topic is not null && !string.IsNullOrEmpty(topic.Id))
            {
                scope["topic"] = topic.Id;
            }

            if (handler is not null)
            {
                scope["handler"] = handler.HandlerType.Name;
            }

            var logger = resolver.GetService<ILoggerFactory>().CreateLogger("Benzene");
            using (logger.BeginScope(scope))
            {
                await next();
            }
        });
    }
}
