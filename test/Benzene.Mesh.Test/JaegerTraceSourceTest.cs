using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Fleet.Jaeger;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The Jaeger-backed trace source: trace-by-id (<c>/api/traces/{id}</c>) mapped from Jaeger's own model
/// (microsecond times, <c>references</c> parentage, <c>processes</c> service names), plus correlation and
/// recent-flows via a per-service search fan-out (<c>/api/traces?service=…</c>), deduped by trace id. The
/// second non-AWS <c>IMeshTraceSource</c>, verified against Jaeger's documented API shapes.
/// </summary>
public class JaegerTraceSourceTest
{
    private const string JaegerUrl = "http://jaeger:16686";
    private static readonly string[] TwoServices = { "orders-api", "billing-api" };

    private sealed class RoutingHandler : HttpMessageHandler
    {
        private readonly Func<string, (HttpStatusCode, string)> _route;
        public int ServiceListCalls { get; private set; }

        public RoutingHandler(Func<string, (HttpStatusCode, string)> route) => _route = route;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/api/services")) ServiceListCalls++;
            var (status, body) = _route(path);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static JaegerTraceSource Source(Func<string, (HttpStatusCode, string)> route,
        string[]? services, out RoutingHandler handler)
    {
        handler = new RoutingHandler(route);
        var options = new JaegerTraceSourceOptions(JaegerUrl) { Services = services };
        return new JaegerTraceSource(new HttpClient(handler), options);
    }

    private static string Tag(string key, string value)
        => "{ \"key\": \"" + key + "\", \"type\": \"string\", \"value\": \"" + value + "\" }";

    private static string Trace(string traceId, string service, string topic, string status,
        string correlationId = "", long startMicros = 1500000000000000, string parentSpanId = "")
    {
        var tags = Tag("benzene.topic", topic) + ", " + Tag("benzene.version", "v1") + ", " + Tag("benzene.status", status);
        if (correlationId.Length > 0) tags += ", " + Tag("benzene.correlation-id", correlationId);
        var references = parentSpanId.Length == 0
            ? "[]"
            : "[ { \"refType\": \"CHILD_OF\", \"spanID\": \"" + parentSpanId + "\" } ]";
        var benzeneSpan = "{ \"spanID\": \"span-" + traceId + "\", \"processID\": \"p1\", \"references\": " + references
            + ", \"startTime\": " + startMicros + ", \"duration\": 400000, \"tags\": [ " + tags + " ] }";
        var otherSpan = "{ \"spanID\": \"other\", \"processID\": \"p1\", \"startTime\": " + (startMicros + 100000)
            + ", \"duration\": 50000, \"tags\": [ " + Tag("http.method", "POST") + " ] }";
        return "{ \"traceID\": \"" + traceId + "\", \"spans\": [ " + benzeneSpan + ", " + otherSpan
            + " ], \"processes\": { \"p1\": { \"serviceName\": \"" + service + "\" } } }";
    }

    private static string Data(params string[] traces) => "{ \"data\": [ " + string.Join(", ", traces) + " ] }";

    private static string ServiceOf(string path)
    {
        var marker = "service=";
        var i = path.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "";
        var rest = path.Substring(i + marker.Length);
        var amp = rest.IndexOf('&');
        return Uri.UnescapeDataString(amp < 0 ? rest : rest.Substring(0, amp));
    }

    [Fact]
    public async Task GetTraceAsync_MapsBenzeneSpans_FromTheJaegerModel()
    {
        var source = Source(
            _ => (HttpStatusCode.OK, Data(Trace("trace-1", "orders-api", "orders:create", "ok", parentSpanId: "parent-1"))),
            TwoServices, out _);

        var view = await source.GetTraceAsync("trace-1");

        Assert.NotNull(view);
        Assert.Equal("trace-1", view!.TraceId);
        var evt = Assert.Single(view.Events); // the benzene span only, not the http.method span
        Assert.Equal("orders:create", evt.Topic);
        Assert.Equal("v1", evt.TopicVersion);
        Assert.Equal("ok", evt.Status);
        Assert.Equal("orders-api", evt.Service);            // from processes[p1].serviceName
        Assert.Equal("span-trace-1", evt.SpanId);
        Assert.Equal("parent-1", evt.ParentSpanId);         // from the CHILD_OF reference
        Assert.Equal(400, evt.DurationMs, 3);               // 400000 µs → ms
    }

    [Fact]
    public async Task GetTraceAsync_UnknownTrace_ReturnsNull()
    {
        var source = Source(_ => (HttpStatusCode.NotFound, ""), TwoServices, out _);
        Assert.Null(await source.GetTraceAsync("nope"));
    }

    [Fact]
    public async Task GetCorrelationAsync_FansOutAcrossServices_AndDedupesByTraceId()
    {
        const string correlationId = "ticket-42";
        // t-a starts before t-b; billing-api's search also returns t-a (a cross-service trace) → dedupe.
        var source = Source(path =>
        {
            Assert.Contains("benzene.correlation-id", Uri.UnescapeDataString(path));
            return ServiceOf(path) switch
            {
                "orders-api" => (HttpStatusCode.OK, Data(Trace("t-a", "orders-api", "orders:create", "ok", correlationId, 1500000000000000))),
                "billing-api" => (HttpStatusCode.OK, Data(
                    Trace("t-b", "billing-api", "billing:charge", "ok", correlationId, 1500000100000000),
                    Trace("t-a", "orders-api", "orders:create", "ok", correlationId, 1500000000000000))),
                _ => (HttpStatusCode.OK, Data())
            };
        }, TwoServices, out _);

        var view = await source.GetCorrelationAsync(correlationId);

        Assert.NotNull(view);
        Assert.Equal(correlationId, view!.CorrelationId);
        Assert.Equal(2, view.Traces.Count);                // t-a deduped despite appearing in both searches
        Assert.Equal("t-a", view.Traces[0].TraceId);       // earliest-first
        Assert.Equal("t-b", view.Traces[1].TraceId);
    }

    [Fact]
    public async Task GetCorrelationAsync_NoMatches_ReturnsNull()
    {
        var source = Source(_ => (HttpStatusCode.OK, Data()), TwoServices, out _);
        Assert.Null(await source.GetCorrelationAsync("nobody"));
    }

    [Fact]
    public async Task GetRecentFlowsAsync_MapsFullTraces_WithEventCountAndFailure_NewestFirst()
    {
        var source = Source(path => ServiceOf(path) switch
        {
            "orders-api" => (HttpStatusCode.OK, Data(Trace("t-a", "orders-api", "orders:create", "ok", startMicros: 1500000000000000))),
            "billing-api" => (HttpStatusCode.OK, Data(Trace("t-b", "billing-api", "billing:charge", "not-found", startMicros: 1500000100000000))),
            _ => (HttpStatusCode.OK, Data())
        }, TwoServices, out _);

        var flows = await source.GetRecentFlowsAsync(20);

        Assert.Equal(2, flows.Count);
        Assert.Equal("t-b", flows[0].TraceId);              // newest first
        Assert.True(flows[0].Failed);                       // status not-found → not the success class
        Assert.Equal(1, flows[0].Events);                   // Jaeger returns full traces → real span count
        Assert.Equal("billing-api", Assert.Single(flows[0].Services));
        Assert.Equal("t-a", flows[1].TraceId);
        Assert.False(flows[1].Failed);                      // status ok
    }

    [Fact]
    public async Task GetRecentFlowsAsync_DiscoversServices_WhenNoneConfigured()
    {
        var source = Source(path =>
        {
            if (path.StartsWith("/api/services")) return (HttpStatusCode.OK, """{ "data": [ "orders-api" ] }""");
            return ServiceOf(path) == "orders-api"
                ? (HttpStatusCode.OK, Data(Trace("t-a", "orders-api", "orders:create", "ok")))
                : (HttpStatusCode.OK, Data());
        }, services: null, out var handler);

        var flows = await source.GetRecentFlowsAsync(20);

        Assert.Equal(1, handler.ServiceListCalls);          // discovery was used
        Assert.Equal("t-a", Assert.Single(flows).TraceId);
    }
}
