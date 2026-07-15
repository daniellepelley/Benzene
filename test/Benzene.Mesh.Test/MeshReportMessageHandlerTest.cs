using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshReportMessageHandlerTest
{
    [Fact]
    public async Task HandleAsync_DelegatesToRegisteredPublisher()
    {
        var publisher = new RecordingMeshReportPublisher();
        var handler = new MeshReportMessageHandler(publisher);
        var report = new MeshServiceReport("payments-fn", DateTimeOffset.UtcNow, "{}", null, null);

        await handler.HandleAsync(report);

        Assert.Same(report, publisher.LastPublished);
    }

    private class RecordingMeshReportPublisher : IMeshReportPublisher
    {
        public MeshServiceReport? LastPublished { get; private set; }

        public Task PublishAsync(MeshServiceReport report)
        {
            LastPublished = report;
            return Task.CompletedTask;
        }
    }
}
