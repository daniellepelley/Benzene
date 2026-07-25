using Amazon.XRay;
using Amazon.XRay.Model;
using Benzene.Mesh.Fleet.Aws.XRay;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The X-Ray-backed trace source: fetch a trace's segments with <c>BatchGetTraces</c> and map its
/// topic-bearing spans into a <see cref="Benzene.Mesh.Collector.TraceView"/> (the fleet UI's
/// waterfall over X-Ray, no push collector). Reads the Benzene attributes the pipeline stamps whether
/// X-Ray landed them as annotations (underscore keys) or metadata (dotted keys), filters out the
/// non-Benzene X-Ray spans, and orders events by start time.
/// </summary>
public class XRayTraceSourceTest
{
    private const string TraceId = "1-581cf771-a006649127e371903a2de979";

    private static Mock<IAmazonXRay> XRay(params string[] segmentDocuments)
    {
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchGetTracesResponse
            {
                Traces = new List<Trace>
                {
                    new Trace
                    {
                        Id = TraceId,
                        Segments = segmentDocuments.Select(d => new Segment { Document = d }).ToList()
                    }
                }
            });
        return mock;
    }

    [Fact]
    public async Task GetTraceAsync_MapsTopicBearingSpans_FromAnnotations()
    {
        // Root segment (orders-api) with a Benzene topic in annotations (X-Ray's underscore-sanitised keys),
        // and a nested subsegment for a downstream topic - the two topic-bearing spans of the flow.
        var segment = """
        {
          "id": "70de5b6f19ff9a0a",
          "name": "orders-api",
          "start_time": 1500000000.5,
          "end_time": 1500000000.9,
          "annotations": {
            "benzene_topic": "orders:create",
            "benzene_version": "v1",
            "benzene_status": "ok",
            "benzene_correlation_id": "ticket-42"
          },
          "subsegments": [
            {
              "id": "aaaabbbbccccdddd",
              "parent_id": "70de5b6f19ff9a0a",
              "name": "orders-api-internal",
              "start_time": 1500000000.6,
              "end_time": 1500000000.7,
              "annotations": {
                "benzene_topic": "inventory:reserve",
                "benzene_status": "not-found",
                "benzene_exception_type": "System.InvalidOperationException"
              }
            }
          ]
        }
        """;

        var source = new XRayTraceSource(XRay(segment).Object);

        var view = await source.GetTraceAsync(TraceId);

        Assert.NotNull(view);
        Assert.Equal(TraceId, view!.TraceId);
        Assert.Equal(2, view.Events.Count);

        // Ordered by start time: the root topic first, the subsegment second.
        var root = view.Events[0];
        Assert.Equal(TraceId, root.TraceId);
        Assert.Equal("70de5b6f19ff9a0a", root.SpanId);
        Assert.Null(root.ParentSpanId);
        Assert.Equal("orders-api", root.Service);
        Assert.Equal("orders:create", root.Topic);
        Assert.Equal("v1", root.TopicVersion);
        Assert.Equal("ok", root.Status);
        Assert.Equal("ticket-42", root.CorrelationId);
        Assert.Equal(400, root.DurationMs, 3); // (0.9 - 0.5) * 1000, within float epoch precision

        var child = view.Events[1];
        Assert.Equal("aaaabbbbccccdddd", child.SpanId);
        Assert.Equal("70de5b6f19ff9a0a", child.ParentSpanId);
        Assert.Equal("orders-api", child.Service); // enclosing segment's name, not a new boundary
        Assert.Equal("inventory:reserve", child.Topic);
        Assert.Equal("not-found", child.Status);
        Assert.Equal("System.InvalidOperationException", child.ExceptionType); // the failure's WHY (spec §3)
        Assert.Null(root.ExceptionType); // absent tag → null, never fabricated
    }

    [Fact]
    public async Task GetTraceAsync_PrefersBenzeneServiceTag_OverTheSegmentName()
    {
        // The bug this fixes: on Lambda the X-Ray segment is named by ADOT after the handler
        // ("ApiGatewayLambdaHandler"), not the service. When the pipeline stamps benzene.service, the mapper
        // must use it as the emitting service, not the infra segment name.
        var segment = """
        {
          "id": "70de5b6f19ff9a0a",
          "name": "ApiGatewayLambdaHandler",
          "start_time": 1500000000.5,
          "end_time": 1500000000.9,
          "annotations": {
            "benzene_topic": "orders:create",
            "benzene_service": "orders-api",
            "benzene_status": "ok"
          }
        }
        """;

        var source = new XRayTraceSource(XRay(segment).Object);

        var view = await source.GetTraceAsync(TraceId);

        var evt = Assert.Single(view!.Events);
        Assert.Equal("orders-api", evt.Service); // the benzene.service tag, not "ApiGatewayLambdaHandler"
    }

    [Fact]
    public async Task GetTraceAsync_FallsBackToSegmentName_WhenNoBenzeneServiceTag()
    {
        // A span that predates the tag: the segment name remains the fallback service.
        var segment = """
        {
          "id": "70de5b6f19ff9a0a",
          "name": "orders-api",
          "start_time": 1500000000.5,
          "end_time": 1500000000.9,
          "annotations": { "benzene_topic": "orders:create", "benzene_status": "ok" }
        }
        """;

        var view = await new XRayTraceSource(XRay(segment).Object).GetTraceAsync(TraceId);

        Assert.Equal("orders-api", Assert.Single(view!.Events).Service);
    }

    [Fact]
    public async Task GetTraceAsync_ReadsBenzeneAttributes_FromNamespacedMetadata()
    {
        // The OTel→X-Ray exporter can land span attributes in metadata under a namespace (dotted keys
        // preserved) rather than annotations - the reader must find them there too.
        var segment = """
        {
          "id": "1111222233334444",
          "name": "payments-api",
          "start_time": 1500000010,
          "end_time": 1500000010.25,
          "metadata": {
            "default": {
              "benzene.topic": "payments:charge",
              "benzene.version": "v2",
              "benzene.status": "unauthorized"
            }
          }
        }
        """;

        var source = new XRayTraceSource(XRay(segment).Object);

        var view = await source.GetTraceAsync(TraceId);

        Assert.NotNull(view);
        var evt = Assert.Single(view!.Events);
        Assert.Equal("payments:charge", evt.Topic);
        Assert.Equal("v2", evt.TopicVersion);
        Assert.Equal("unauthorized", evt.Status);
        Assert.Equal("payments-api", evt.Service);
        Assert.Equal(250, evt.DurationMs, 3);
    }

    [Fact]
    public async Task GetTraceAsync_SkipsNonBenzeneSpans()
    {
        // A real X-Ray trace mixes in transport/AWS-SDK spans with no Benzene topic - those are not mesh
        // flow events and must not appear in the waterfall.
        var segment = """
        {
          "id": "5555666677778888",
          "name": "orders-api",
          "start_time": 1500000000.0,
          "end_time": 1500000001.0,
          "annotations": { "benzene_topic": "orders:get-all", "benzene_status": "ok" },
          "subsegments": [
            {
              "id": "9999aaaabbbbcccc",
              "name": "DynamoDB",
              "start_time": 1500000000.1,
              "end_time": 1500000000.2,
              "namespace": "aws"
            }
          ]
        }
        """;

        var source = new XRayTraceSource(XRay(segment).Object);

        var view = await source.GetTraceAsync(TraceId);

        Assert.NotNull(view);
        var evt = Assert.Single(view!.Events); // only the topic-bearing span, not the DynamoDB subsegment
        Assert.Equal("orders:get-all", evt.Topic);
    }

    [Fact]
    public async Task GetTraceAsync_UnknownTrace_ReturnsNull()
    {
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchGetTracesResponse
            {
                Traces = new List<Trace>(),
                UnprocessedTraceIds = new List<string> { TraceId }
            });
        var source = new XRayTraceSource(mock.Object);

        Assert.Null(await source.GetTraceAsync(TraceId));
    }

    [Fact]
    public async Task GetTraceAsync_TraceWithoutBenzeneSpans_ReturnsNull()
    {
        // A real trace that carried no Benzene topic-bearing span is not a mesh flow - NotFound, not an
        // empty zero-event waterfall.
        var segment = """
        { "id": "deadbeefdeadbeef", "name": "some-other-service", "start_time": 1500000000.0, "end_time": 1500000001.0 }
        """;
        var source = new XRayTraceSource(XRay(segment).Object);

        Assert.Null(await source.GetTraceAsync(TraceId));
    }

    [Fact]
    public async Task GetTraceAsync_EmptyTraceId_ReturnsNull()
    {
        var source = new XRayTraceSource(new Mock<IAmazonXRay>().Object);
        Assert.Null(await source.GetTraceAsync(""));
    }

    private static string Segment(string id, string service, string topic, string correlationId, double start) => $$"""
        {
          "id": "{{id}}",
          "name": "{{service}}",
          "start_time": {{start.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
          "end_time": {{(start + 0.1).ToString(System.Globalization.CultureInfo.InvariantCulture)}},
          "annotations": {
            "benzene_topic": "{{topic}}",
            "benzene_status": "ok",
            "benzene_correlation_id": "{{correlationId}}"
          }
        }
        """;

    [Fact]
    public async Task GetCorrelationAsync_FindsMatchingTraces_GroupedByTrace()
    {
        // Two distinct traces both carrying the same business correlation id (ticket-42) - a correlation
        // id can span more than one flow, so each comes back as its own TraceView.
        const string correlationId = "ticket-42";
        const string traceA = "1-aaaaaaaa-1111";
        const string traceB = "1-bbbbbbbb-2222";

        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTraceSummariesRequest req, CancellationToken _) =>
            {
                // The search filters on the correlation-id annotation over a time window.
                Assert.Contains("annotation.benzene_correlation_id", req.FilterExpression);
                Assert.Contains(correlationId, req.FilterExpression);
                return new GetTraceSummariesResponse
                {
                    TraceSummaries = new List<TraceSummary>
                    {
                        new TraceSummary { Id = traceB }, // later trace returned first...
                        new TraceSummary { Id = traceA }
                    }
                };
            });
        mock.Setup(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchGetTracesRequest req, CancellationToken _) => new BatchGetTracesResponse
            {
                Traces = req.TraceIds.Select(id => new Trace
                {
                    Id = id,
                    Segments = new List<Segment>
                    {
                        new Segment
                        {
                            Document = id == traceA
                                ? Segment("seg-a", "orders-api", "orders:create", correlationId, 1500000000.0)
                                : Segment("seg-b", "billing-api", "billing:charge", correlationId, 1500000100.0)
                        }
                    }
                }).ToList()
            });

        var source = new XRayTraceSource(mock.Object);

        var view = await source.GetCorrelationAsync(correlationId);

        Assert.NotNull(view);
        Assert.Equal(correlationId, view!.CorrelationId);
        Assert.Equal(2, view.Traces.Count);
        // Ordered earliest-first regardless of the order X-Ray returned the summaries.
        Assert.Equal(traceA, view.Traces[0].TraceId);
        Assert.Equal("orders:create", view.Traces[0].Events.Single().Topic);
        Assert.Equal(traceB, view.Traces[1].TraceId);
        Assert.Equal("billing:charge", view.Traces[1].Events.Single().Topic);
    }

    [Fact]
    public async Task GetCorrelationAsync_PagesThroughAllSummaries()
    {
        const string correlationId = "ticket-99";
        var mock = new Mock<IAmazonXRay>();
        var call = 0;
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTraceSummariesRequest req, CancellationToken _) =>
            {
                // First page returns a NextToken; the source must follow it to collect page two.
                call++;
                return call == 1
                    ? new GetTraceSummariesResponse
                    {
                        TraceSummaries = new List<TraceSummary> { new TraceSummary { Id = "1-page1-0001" } },
                        NextToken = "more"
                    }
                    : new GetTraceSummariesResponse
                    {
                        TraceSummaries = new List<TraceSummary> { new TraceSummary { Id = "1-page2-0002" } }
                    };
            });
        mock.Setup(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchGetTracesRequest req, CancellationToken _) => new BatchGetTracesResponse
            {
                Traces = req.TraceIds.Select((id, i) => new Trace
                {
                    Id = id,
                    Segments = new List<Segment>
                    {
                        new Segment { Document = Segment($"seg-{i}", "svc", "topic:do", correlationId, 1500000000.0 + i) }
                    }
                }).ToList()
            });

        var source = new XRayTraceSource(mock.Object);

        var view = await source.GetCorrelationAsync(correlationId);

        Assert.NotNull(view);
        Assert.Equal(2, view!.Traces.Count); // both pages' traces collected
        Assert.Equal(2, call); // followed the NextToken
    }

    [Fact]
    public async Task GetCorrelationAsync_NoMatches_ReturnsNull()
    {
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTraceSummariesResponse { TraceSummaries = new List<TraceSummary>() });
        var source = new XRayTraceSource(mock.Object);

        Assert.Null(await source.GetCorrelationAsync("nobody"));
    }

    [Fact]
    public async Task GetCorrelationAsync_EmptyCorrelationId_ReturnsNull()
    {
        var source = new XRayTraceSource(new Mock<IAmazonXRay>().Object);
        Assert.Null(await source.GetCorrelationAsync(""));
    }

    // A minimal topic-bearing segment document carrying a benzene.service tag and a real start_time,
    // for the recent-flows enrichment tests (which map the fetched trace to read benzene.service).
    private static string FlowSegment(string service, string topic, double startSeconds, double endSeconds) => $$"""
    {
      "id": "{{service.Replace("-", "")}}0001",
      "name": "{{service}}-lambda-infra",
      "start_time": {{startSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
      "end_time": {{endSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
      "annotations": {
        "benzene_topic": "{{topic}}",
        "benzene_service": "{{service}}",
        "benzene_status": "ok"
      }
    }
    """;

    [Fact]
    public async Task GetRecentFlowsAsync_EnrichesServices_FromBenzeneService_NotXRayServiceIds()
    {
        // The X-Ray summary's ServiceIds carry the infra/handler names (what defect 2 was showing);
        // enrichment fetches the trace and reads benzene.service instead, so the row shows the real name.
        var trace = "1-5c000000-aaaaaaaaaaaaaaaaaaaaaaaa";
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTraceSummariesRequest req, CancellationToken _) =>
            {
                Assert.Null(req.FilterExpression); // recent flows is unfiltered
                return new GetTraceSummariesResponse
                {
                    TraceSummaries = new List<Amazon.XRay.Model.TraceSummary>
                    {
                        new Amazon.XRay.Model.TraceSummary
                        {
                            Id = trace, Duration = 0.25, HasError = true, HasFault = false,
                            // The infra name X-Ray reports — must NOT be what the row shows.
                            ServiceIds = new List<ServiceId> { new ServiceId { Name = "EventBridgeEventHandler" } }
                        }
                    }
                };
            });
        mock.Setup(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchGetTracesResponse
            {
                Traces = new List<Trace>
                {
                    new Trace { Id = trace, Segments = new List<Segment>
                        { new Segment { Document = FlowSegment("benzene-mesh", "benzene:mesh:aggregate", 1_600_000_000.5, 1_600_000_000.75) } } }
                }
            });

        var flows = await new XRayTraceSource(mock.Object).GetRecentFlowsAsync(20);

        var flow = Assert.Single(flows);
        Assert.Equal("benzene-mesh", Assert.Single(flow.Services)); // benzene.service, not "EventBridgeEventHandler"
        Assert.Equal(1, flow.Events);                                // real span count, not the old hardcoded 0
        Assert.True(flow.Failed);                                    // summary's HasError flag is kept
        Assert.Equal(250, flow.DurationMs, 3);                       // summary duration (seconds → ms)
        Assert.Equal("benzene:mesh:aggregate", flow.Topic);                  // entry topic from the earliest mapped event
    }

    [Fact]
    public async Task GetRecentFlowsAsync_OrdersByRealStart_WithinTheSameEpochSecond()
    {
        // Both trace ids share the SAME epoch prefix (second granularity), so the id-epoch key alone can't
        // order them. Enrichment reads each trace's real millisecond start, so the later-starting flow sorts
        // first even though the ids tie on the second.
        var a = "1-5c000000-aaaaaaaaaaaaaaaaaaaaaaaa";
        var b = "1-5c000000-bbbbbbbbbbbbbbbbbbbbbbbb";
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTraceSummariesResponse
            {
                TraceSummaries = new List<Amazon.XRay.Model.TraceSummary>
                {
                    new Amazon.XRay.Model.TraceSummary { Id = a, Duration = 0.1 },
                    new Amazon.XRay.Model.TraceSummary { Id = b, Duration = 0.1 }
                }
            });
        mock.Setup(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchGetTracesResponse
            {
                Traces = new List<Trace>
                {
                    // a starts at .200, b starts at .800 within the same second — b is newer.
                    new Trace { Id = a, Segments = new List<Segment>
                        { new Segment { Document = FlowSegment("orders-api", "orders:create", 1_600_000_000.200, 1_600_000_000.3) } } },
                    new Trace { Id = b, Segments = new List<Segment>
                        { new Segment { Document = FlowSegment("billing-api", "billing:charge", 1_600_000_000.800, 1_600_000_000.9) } } }
                }
            });

        var flows = await new XRayTraceSource(mock.Object).GetRecentFlowsAsync(20);

        Assert.Equal(new[] { b, a }, flows.Select(f => f.TraceId).ToArray()); // later real start first
    }

    [Fact]
    public async Task GetRecentFlowsAsync_FallsBackToSummaryPlane_PerRow_WhenEnrichmentFails()
    {
        // One trace maps to a Benzene span (enriched); the other isn't returned by BatchGetTraces at all
        // (e.g. aged out) — that row degrades to the summary plane (ServiceIds name, Events = 0), it doesn't
        // vanish or blank the list.
        var enriched = "1-5c000000-aaaaaaaaaaaaaaaaaaaaaaaa";
        var missing = "1-5b000000-bbbbbbbbbbbbbbbbbbbbbbbb";
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTraceSummariesResponse
            {
                TraceSummaries = new List<Amazon.XRay.Model.TraceSummary>
                {
                    new Amazon.XRay.Model.TraceSummary { Id = enriched, Duration = 0.1 },
                    new Amazon.XRay.Model.TraceSummary
                    {
                        Id = missing, Duration = 0.2,
                        ServiceIds = new List<ServiceId> { new ServiceId { Name = "orders-api" } }
                    }
                }
            });
        mock.Setup(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchGetTracesResponse
            {
                // Only the enriched trace comes back; the missing one is absent (UnprocessedTraceIds in X-Ray).
                Traces = new List<Trace>
                {
                    new Trace { Id = enriched, Segments = new List<Segment>
                        { new Segment { Document = FlowSegment("benzene-mesh", "benzene:mesh:aggregate", 1_600_000_000.5, 1_600_000_000.6) } } }
                }
            });

        var flows = await new XRayTraceSource(mock.Object).GetRecentFlowsAsync(20);

        Assert.Equal(2, flows.Count);
        Assert.Equal(enriched, flows[0].TraceId);                    // newer, enriched
        Assert.Equal("benzene-mesh", Assert.Single(flows[0].Services));
        Assert.Equal(1, flows[0].Events);
        Assert.Equal(missing, flows[1].TraceId);                     // older, summary-plane fallback
        Assert.Equal("orders-api", Assert.Single(flows[1].Services)); // ServiceIds name
        Assert.Equal(0, flows[1].Events);                            // no span count on the summary plane
        Assert.Null(flows[1].Topic);                                 // no Benzene span mapped → no topic attribution
    }

    [Fact]
    public async Task GetRecentFlowsAsync_EnrichmentOff_ReproducesSummaryPlane_WithoutFetchingTraces()
    {
        // With enrichment disabled (RecentFlowsServiceEnrichmentMax = 0), the source is the pre-enrichment
        // behavior: ServiceIds names, id-epoch ordering, Events = 0, and zero BatchGetTraces calls.
        var older = "1-5b000000-aaaaaaaaaaaaaaaaaaaaaaaa";
        var newer = "1-5c000000-bbbbbbbbbbbbbbbbbbbbbbbb";
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTraceSummariesResponse
            {
                TraceSummaries = new List<Amazon.XRay.Model.TraceSummary>
                {
                    new Amazon.XRay.Model.TraceSummary
                    {
                        Id = older, Duration = 0.4, HasError = false, HasFault = false,
                        ServiceIds = new List<ServiceId> { new ServiceId { Name = "orders-api" } }
                    },
                    new Amazon.XRay.Model.TraceSummary
                    {
                        Id = newer, Duration = 0.25, HasError = true, HasFault = false,
                        ServiceIds = new List<ServiceId> { new ServiceId { Name = "billing-api" } }
                    }
                }
            });

        var source = new XRayTraceSource(mock.Object,
            new XRayTraceSourceOptions { RecentFlowsServiceEnrichmentMax = 0 });

        var flows = await source.GetRecentFlowsAsync(20);

        Assert.Equal(2, flows.Count);
        Assert.Equal(newer, flows[0].TraceId);       // newest first (id epoch)
        Assert.True(flows[0].Failed);
        Assert.Equal(250, flows[0].DurationMs, 3);
        Assert.Equal("billing-api", Assert.Single(flows[0].Services)); // ServiceIds name, not enriched
        Assert.Equal(0, flows[0].Events);
        Assert.Equal(older, flows[1].TraceId);
        mock.Verify(x => x.BatchGetTracesAsync(It.IsAny<BatchGetTracesRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRecentFlowsAsync_HonoursTheLimit()
    {
        var mock = new Mock<IAmazonXRay>();
        mock.Setup(x => x.GetTraceSummariesAsync(It.IsAny<GetTraceSummariesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTraceSummariesResponse
            {
                TraceSummaries = Enumerable.Range(0, 5).Select(i =>
                    new Amazon.XRay.Model.TraceSummary { Id = $"1-5b0000{i:D2}-{i:D24}" }).ToList()
            });
        // Enrichment off keeps this focused on the limit (no BatchGetTraces plumbing needed).
        var source = new XRayTraceSource(mock.Object,
            new XRayTraceSourceOptions { RecentFlowsServiceEnrichmentMax = 0 });

        var flows = await source.GetRecentFlowsAsync(3);

        Assert.Equal(3, flows.Count);
    }
}
