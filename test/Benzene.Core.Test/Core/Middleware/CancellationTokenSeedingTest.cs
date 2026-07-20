using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Middleware;

/// <summary>
/// Coverage for seeding the scope's ambient <see cref="ICancellationTokenAccessor"/>: the
/// <see cref="MiddlewareApplication{TEvent,TContext}"/> token overload sets it so a component resolved
/// during the pipeline observes the transport's cancellation token, while the original no-token
/// overload leaves it at <see cref="CancellationToken.None"/>. Also covers the
/// <see cref="CancellationTokenAccessorExtensions.SeedCancellationToken"/> helper directly.
/// </summary>
public class CancellationTokenSeedingTest
{
    private sealed class CapturingPipeline<TContext> : IMiddlewarePipeline<TContext>
    {
        public CancellationToken Observed { get; private set; }

        public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
        {
            Observed = serviceResolver.GetService<ICancellationTokenAccessor>().CancellationToken;
            return Task.CompletedTask;
        }
    }

    private static IServiceResolverFactory CreateFactory()
    {
        var services = new ServiceCollection();
        services.AddScoped<CancellationTokenAccessor>();
        services.AddScoped<ICancellationTokenAccessor>(x => x.GetService<CancellationTokenAccessor>());
        return new MicrosoftServiceResolverFactory(services);
    }

    [Fact]
    public async Task HandleAsync_WithToken_SeedsTheAccessorForThePipeline()
    {
        using var cts = new CancellationTokenSource();
        var pipeline = new CapturingPipeline<string>();
        var application = new MiddlewareApplication<string, string>(pipeline, e => e);

        await application.HandleAsync("event", CreateFactory(), cts.Token);

        Assert.Equal(cts.Token, pipeline.Observed);
        Assert.True(pipeline.Observed.CanBeCanceled);
    }

    [Fact]
    public async Task HandleAsync_WithoutToken_LeavesTheAccessorAtNone()
    {
        var pipeline = new CapturingPipeline<string>();
        var application = new MiddlewareApplication<string, string>(pipeline, e => e);

        await application.HandleAsync("event", CreateFactory());

        Assert.Equal(CancellationToken.None, pipeline.Observed);
    }

    [Fact]
    public async Task HandleAsync_WithResult_WithToken_SeedsTheAccessor()
    {
        using var cts = new CancellationTokenSource();
        var pipeline = new CapturingPipeline<string>();
        var application = new MiddlewareApplication<string, string, string>(pipeline, e => e, c => c);

        await application.HandleAsync("event", CreateFactory(), cts.Token);

        Assert.Equal(cts.Token, pipeline.Observed);
    }

    [Fact]
    public void SeedCancellationToken_WithRealToken_SetsTheAccessor()
    {
        using var cts = new CancellationTokenSource();
        var resolver = CreateFactory().CreateScope();

        resolver.SeedCancellationToken(cts.Token);

        Assert.Equal(cts.Token, resolver.GetService<ICancellationTokenAccessor>().CancellationToken);
    }

    [Fact]
    public void SeedCancellationToken_WithNone_LeavesTheAccessorAtNone()
    {
        var resolver = CreateFactory().CreateScope();

        resolver.SeedCancellationToken(CancellationToken.None);

        Assert.Equal(CancellationToken.None, resolver.GetService<ICancellationTokenAccessor>().CancellationToken);
    }

    [Fact]
    public void SeedCancellationToken_WhenNoAccessorRegistered_DoesNotThrow()
    {
        using var cts = new CancellationTokenSource();
        var resolver = new MicrosoftServiceResolverFactory(new ServiceCollection()).CreateScope();

        // No CancellationTokenAccessor is registered; seeding must be a safe no-op.
        resolver.SeedCancellationToken(cts.Token);
    }
}
