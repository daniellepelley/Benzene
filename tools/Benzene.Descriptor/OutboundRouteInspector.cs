using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;

namespace Benzene.Descriptor;

/// <summary>
/// Recovers the outbound <b>transport kind</b> per routed topic (e.g. <c>sqs</c>, <c>sns</c>,
/// <c>eventbridge</c>, <c>servicebus</c>) from a built service — cloud-agnostically, by reading the
/// transport off the route's context-converter type name (every transport follows the
/// <c>XxxSendMessageContext</c> convention, so no cloud-specific list is hard-coded).
///
/// SPIKE-GRADE: this reaches into the built outbound routing table by reflection because today's
/// outbound model retains no introspectable transport/destination read-model. It deliberately does
/// NOT surface the destination — that value is resolved (env-var name lost) and is the crux of the
/// paused outbound-routing redesign. When that lands with a proper read-model, delete this and read
/// it directly. Any reflection failure degrades to "unknown" rather than throwing.
/// </summary>
internal static class OutboundRouteInspector
{
    public static IReadOnlyDictionary<string, string> TransportsByTopic(IServiceResolver resolver)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var sender = resolver.TryGetService<IBenzeneMessageSender>();
            if (sender is null) return result;

            var routes = sender.GetType()
                .GetField("_routes", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(sender) as IReadOnlyDictionary<string, IMiddlewarePipeline<OutboundContext>>;
            if (routes is null) return result;

            foreach (var (topic, pipeline) in routes)
                result[topic] = TransportOf(pipeline, resolver);
        }
        catch
        {
            // Best-effort only — never fail descriptor emission over outbound introspection.
        }
        return result;
    }

    private static string TransportOf(IMiddlewarePipeline<OutboundContext> pipeline, IServiceResolver resolver)
    {
        try
        {
            var items = pipeline.GetType()
                .GetField("_reversedItems", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(pipeline) as Func<IServiceResolver, IMiddleware<OutboundContext>>[];
            if (items is null) return "unknown";

            foreach (var factory in items)
            {
                var middleware = factory(resolver);
                var type = middleware.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ContextConverterMiddleware<,>))
                {
                    var target = type.GetGenericArguments()[1]; // TContextOut, e.g. SqsSendMessageContext
                    return ToTransportName(target.Name);
                }
            }
        }
        catch
        {
            // fall through to unknown
        }
        return "unknown";
    }

    // "SqsSendMessageContext" -> "sqs"; "ServiceBusSendMessageContext" -> "servicebus". Convention-based,
    // so any transport that follows it works without a hard-coded per-cloud map.
    private static string ToTransportName(string contextTypeName)
    {
        const string suffix = "SendMessageContext";
        var name = contextTypeName.EndsWith(suffix, StringComparison.Ordinal)
            ? contextTypeName[..^suffix.Length]
            : contextTypeName;
        return name.ToLowerInvariant();
    }
}
