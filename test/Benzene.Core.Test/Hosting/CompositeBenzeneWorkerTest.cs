using System;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.SelfHost;
using Xunit;

namespace Benzene.Test.Hosting;

public class CompositeBenzeneWorkerTest
{
    private class FakeWorker : IBenzeneWorker
    {
        private readonly bool _throwOnStart;
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }

        public FakeWorker(bool throwOnStart = false)
        {
            _throwOnStart = throwOnStart;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_throwOnStart)
            {
                throw new InvalidOperationException("boom");
            }

            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stopped = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task StartAsync_WhenAWorkerFails_RollsBackTheStartedWorkers()
    {
        var good = new FakeWorker();
        var bad = new FakeWorker(throwOnStart: true);
        var composite = new CompositeBenzeneWorker(new IBenzeneWorker[] { good, bad });

        await Assert.ThrowsAsync<InvalidOperationException>(() => composite.StartAsync(CancellationToken.None));

        Assert.True(good.Started);
        Assert.True(good.Stopped); // rolled back so a partial start doesn't leak a running worker
    }

    [Fact]
    public async Task StartAsync_WhenAllSucceed_DoesNotStopAnyWorker()
    {
        var first = new FakeWorker();
        var second = new FakeWorker();
        var composite = new CompositeBenzeneWorker(new IBenzeneWorker[] { first, second });

        await composite.StartAsync(CancellationToken.None);

        Assert.True(first.Started);
        Assert.True(second.Started);
        Assert.False(first.Stopped);
        Assert.False(second.Stopped);
    }
}
