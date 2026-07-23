using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients;

public class ParallelOutboundTest
{
    private static IBenzeneMessageSender SenderFor(Action<OutboundRoutingBuilder> configure)
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(configure);
        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        return resolver.GetService<IBenzeneMessageSender>();
    }

    [Fact]
    public async Task UseParallel_RunsBranchesConcurrently_NotOneAfterAnother()
    {
        var live = 0;
        var maxLive = 0;
        var gate = new object();

        Func<IServiceResolver, Func<OutboundContext, Func<Task>, Task>> branch = _ => async (ctx, _) =>
        {
            lock (gate) { maxLive = Math.Max(maxLive, ++live); }
            await Task.Delay(60);
            lock (gate) { live--; }
            ctx.Response = BenzeneResult.Ok<Void>();
        };

        var sender = SenderFor(routing => routing.Route("order:create", p => p.UseParallel(
            ("a", b => b.Use("a", branch)),
            ("b", b => b.Use("b", branch)))));

        var stopwatch = Stopwatch.StartNew();
        var result = await sender.SendAsync<string, Void>("order:create", "payload");
        stopwatch.Stop();

        Assert.True(result.IsSuccessful);
        Assert.Equal(2, maxLive);                          // both branches were in flight at once
        Assert.True(stopwatch.ElapsedMilliseconds < 120,   // ~max(60,60), not 120 sequential
            $"expected concurrent (~60ms) not sequential (~120ms), was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task UseParallel_AllBranchesSucceed_AggregatesToSuccess()
    {
        var sender = SenderFor(routing => routing.Route("order:create", p => p.UseParallel(
            ("sqs", b => b.OnRequest(ctx => ctx.Response = BenzeneResult.Ok<Void>())),
            ("sns", b => b.OnRequest(ctx => ctx.Response = BenzeneResult.Ok<Void>())))));

        var result = await sender.SendAsync<string, Void>("order:create", "payload");

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task UseParallel_OneBranchThrows_FailsAndNamesIt_ButStillRunsTheOthers()
    {
        var snsRan = false;

        var sender = SenderFor(routing => routing.Route("order:create", p => p.UseParallel(
            ("sqs", b => b.Use("sqs", _ => async (ctx, _) =>
            {
                await Task.Yield();
                throw new InvalidOperationException("access denied");
            })),
            ("sns", b => b.Use("sns", _ => async (ctx, _) =>
            {
                await Task.Yield();
                snsRan = true;
                ctx.Response = BenzeneResult.Ok<Void>();
            })))));

        var result = await sender.SendAsync<string, Void>("order:create", "payload");

        Assert.False(result.IsSuccessful);
        Assert.True(snsRan); // a failing branch must not abort the fan-out
        Assert.Contains(result.Errors, e => e.Contains("sqs") && e.Contains("access denied"));
    }

    [Fact]
    public async Task UseParallel_OneBranchReturnsFailureResult_FailsAndNamesIt()
    {
        var sender = SenderFor(routing => routing.Route("order:create", p => p.UseParallel(
            ("sqs", b => b.OnRequest(ctx => ctx.Response = BenzeneResult.Ok<Void>())),
            ("sns", b => b.OnRequest(ctx => ctx.Response = BenzeneResult.Set<Void>(BenzeneResultStatus.ServiceUnavailable))))));

        var result = await sender.SendAsync<string, Void>("order:create", "payload");

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Errors, e => e.Contains("sns"));
    }
}
