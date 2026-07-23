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
                "benzene_status": "not-found"
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
}
