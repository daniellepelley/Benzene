using System.Threading;
using System.Threading.Tasks;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Xunit;

namespace Benzene.Test.HealthChecks;

public class ShutdownReadinessHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_NotShuttingDown_ReturnsHealthy()
    {
        var result = await new ShutdownReadinessHealthCheck(new ShutdownState()).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Shutdown", result.Type);
        Assert.Equal(false, result.Data["ShuttingDown"]);
    }

    [Fact]
    public async Task ExecuteAsync_AfterMarkShuttingDown_ReturnsFailed()
    {
        var state = new ShutdownState();
        state.MarkShuttingDown();

        var result = await new ShutdownReadinessHealthCheck(state).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(true, result.Data["ShuttingDown"]);
    }

    [Fact]
    public async Task LinkTo_CancelledToken_TripsTheLatch()
    {
        using var cts = new CancellationTokenSource();
        var state = new ShutdownState().LinkTo(cts.Token);

        Assert.False(state.IsShuttingDown);

        cts.Cancel();

        Assert.True(state.IsShuttingDown);
        var result = await new ShutdownReadinessHealthCheck(state).ExecuteAsync();
        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }

    [Fact]
    public void LinkTo_AlreadyCancelledToken_TripsImmediately()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var state = new ShutdownState().LinkTo(cts.Token);

        Assert.True(state.IsShuttingDown);
    }

    [Fact]
    public void MarkShuttingDown_Latches()
    {
        var state = new ShutdownState();
        state.MarkShuttingDown();
        state.MarkShuttingDown();

        Assert.True(state.IsShuttingDown);
    }
}
