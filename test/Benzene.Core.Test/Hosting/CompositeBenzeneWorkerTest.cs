using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.SelfHost;
using Moq;
using Xunit;

namespace Benzene.Test.Hosting;

public class CompositeBenzeneWorkerTest
{
    /// <summary>
    /// Regression: the composite must materialize its worker sequence once, so <c>StopAsync</c> stops
    /// the very instances <c>StartAsync</c> started. It's constructed from a <em>deferred</em> query
    /// (<c>BenzeneWorkerBuilder.Create</c> passes <c>_apps.Select(f =&gt; f(resolver))</c>, and each
    /// factory news up a fresh worker); re-enumerating in StopAsync would build a second, never-started
    /// worker set and stop those instead - silently skipping every real worker's drain/close/commit.
    /// </summary>
    [Fact]
    public async Task StopAsync_StopsTheSameWorkerInstancesThatStartAsyncStarted()
    {
        var startedIds = new List<int>();
        var stoppedIds = new List<int>();
        var nextId = 0;

        IBenzeneWorker MakeWorker()
        {
            var id = nextId++;
            var mock = new Mock<IBenzeneWorker>();
            mock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
                .Returns(() => { startedIds.Add(id); return Task.CompletedTask; });
            mock.Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
                .Returns(() => { stoppedIds.Add(id); return Task.CompletedTask; });
            return mock.Object;
        }

        // Deferred: re-enumerating would call MakeWorker again, minting new instances - exactly the
        // shape BenzeneWorkerBuilder.Create produces.
        var deferred = Enumerable.Range(0, 2).Select(_ => MakeWorker());
        var composite = new CompositeBenzeneWorker(deferred);

        await composite.StartAsync(CancellationToken.None);
        await composite.StopAsync(CancellationToken.None);

        Assert.Equal(2, nextId); // only two workers were ever built (not four)
        Assert.Equal(new[] { 0, 1 }, startedIds);
        Assert.Equal(new[] { 0, 1 }, stoppedIds); // the SAME instances, not a fresh set {2,3}
    }
}
