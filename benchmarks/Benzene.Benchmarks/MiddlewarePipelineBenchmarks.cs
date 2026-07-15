using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Benchmarks;

/// <summary>
/// Benchmarks <see cref="MiddlewarePipeline{TContext}.HandleAsync"/> - the per-request/per-message
/// middleware chain construction path. Two variants, deliberately not conflated into one number:
/// <see cref="HandleAsync_ChainConstructionOnly"/> isolates the chain-construction cost alone
/// (a long-lived <see cref="IServiceResolver"/> is reused across calls), while
/// <see cref="HandleAsync_WithScopeCreation"/> also creates and disposes a fresh DI scope per call,
/// matching how every real transport adapter actually invokes <c>CreateScope()</c> once per
/// request/message. <see cref="MiddlewareCount"/> is parameterized because the cost this suite
/// targets (each middleware's <c>IMiddlewareFactory.Create</c> call, and previously an
/// <c>Enumerable.Reverse()</c> over the whole array on every call) scales with chain length.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class MiddlewarePipelineBenchmarks
{
    [Params(1, 5, 20)]
    public int MiddlewareCount { get; set; }

    private MicrosoftServiceResolverFactory _resolverFactory = null!;
    private IServiceResolver _resolver = null!;
    private IMiddlewarePipeline<object> _pipeline = null!;
    private readonly object _context = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        for (var i = 0; i < MiddlewareCount; i++)
        {
            builder.Use((_, _) => Task.CompletedTask);
        }

        _pipeline = builder.Build();

        _resolverFactory = new MicrosoftServiceResolverFactory(services);
        _resolver = _resolverFactory.CreateScope();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _resolver.Dispose();
        _resolverFactory.Dispose();
    }

    [Benchmark(Baseline = true, Description = "HandleAsync only (chain construction, shared resolver)")]
    public Task HandleAsync_ChainConstructionOnly()
        => _pipeline.HandleAsync(_context, _resolver);

    [Benchmark(Description = "HandleAsync with a fresh scope per call (realistic per-request cost)")]
    public async Task HandleAsync_WithScopeCreation()
    {
        using var resolver = _resolverFactory.CreateScope();
        await _pipeline.HandleAsync(_context, resolver);
    }
}
