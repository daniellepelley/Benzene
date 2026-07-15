using Benzene.HealthChecks.Core;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Reporting;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshSelfReportMiddlewareTest
{
    [Fact]
    public async Task HandleAsync_CallsNext()
    {
        var nextCalled = false;
        var middleware = BuildMiddleware(out _, out _);

        await middleware.HandleAsync("context", () => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_FirstCall_PublishesOpportunistically()
    {
        var middleware = BuildMiddleware(out var publisher, out _);

        await middleware.HandleAsync("context", () => Task.CompletedTask);
        await publisher.Signal.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(publisher.LastPublished);
        Assert.Equal("orders-api", publisher.LastPublished!.Name);
    }

    [Fact]
    public async Task HandleAsync_SecondCallWithinMinimumInterval_DoesNotPublishAgain()
    {
        var middleware = BuildMiddleware(out var publisher, out _, minimumInterval: TimeSpan.FromMinutes(5));

        await middleware.HandleAsync("context", () => Task.CompletedTask);
        await publisher.Signal.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, publisher.PublishCount);

        publisher.Signal = new TaskCompletionSource();
        await middleware.HandleAsync("context", () => Task.CompletedTask);

        // No second signal will ever arrive since the throttle should have skipped this call -
        // a short delay confirms the count didn't advance, rather than asserting on a signal that
        // (correctly) never fires.
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, publisher.PublishCount);
    }

    [Fact]
    public async Task HandleAsync_PublisherThrows_DoesNotPropagate()
    {
        var publisher = new FailingMeshReportPublisher();
        var options = new MeshSelfReportOptions(
            "orders-api",
            () => Task.FromResult<string?>("{}"),
            () => Task.FromResult<HealthCheckResponse?>(null));
        var middleware = new MeshSelfReportMiddleware<string>(publisher, options, new MeshSelfReportState());

        // Should not throw, even though the injected publisher always throws.
        await middleware.HandleAsync("context", () => Task.CompletedTask);
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        Assert.True(publisher.WasCalled);
    }

    [Fact]
    public async Task HandleAsync_DoesNotBlockOnASlowPublisher()
    {
        var neverCompletes = new TaskCompletionSource();
        var stallingPublisher = new StallingMeshReportPublisher(neverCompletes.Task);
        var options = new MeshSelfReportOptions(
            "orders-api",
            () => Task.FromResult<string?>("{}"),
            () => Task.FromResult<HealthCheckResponse?>(null));
        var middleware = new MeshSelfReportMiddleware<string>(stallingPublisher, options, new MeshSelfReportState());

        var handleTask = middleware.HandleAsync("context", () => Task.CompletedTask);
        var completed = await Task.WhenAny(handleTask, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(handleTask, completed);
    }

    private static MeshSelfReportMiddleware<string> BuildMiddleware(
        out RecordingMeshReportPublisher publisher, out MeshSelfReportState state, TimeSpan? minimumInterval = null)
    {
        publisher = new RecordingMeshReportPublisher();
        state = new MeshSelfReportState();
        var options = new MeshSelfReportOptions(
            "orders-api",
            () => Task.FromResult<string?>("{\"info\":{\"title\":\"orders-api\"}}"),
            () => Task.FromResult<HealthCheckResponse?>(null),
            minimumInterval);
        return new MeshSelfReportMiddleware<string>(publisher, options, state);
    }

    private class RecordingMeshReportPublisher : IMeshReportPublisher
    {
        public TaskCompletionSource Signal = new();
        public MeshServiceReport? LastPublished { get; private set; }
        public int PublishCount { get; private set; }

        public Task PublishAsync(MeshServiceReport report)
        {
            LastPublished = report;
            PublishCount++;
            Signal.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private class FailingMeshReportPublisher : IMeshReportPublisher
    {
        public bool WasCalled { get; private set; }

        public Task PublishAsync(MeshServiceReport report)
        {
            WasCalled = true;
            throw new InvalidOperationException("publish always fails in this test");
        }
    }

    private class StallingMeshReportPublisher : IMeshReportPublisher
    {
        private readonly Task _stall;

        public StallingMeshReportPublisher(Task stall)
        {
            _stall = stall;
        }

        public Task PublishAsync(MeshServiceReport report) => _stall;
    }
}
