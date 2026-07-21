using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Benchmarks;

/// <summary>
/// Measures the steady-state per-message cost the per-middleware Activity tracing wrapper
/// (<c>AddDiagnostics()</c>/<c>AddActivityPerMiddleware()</c>) adds, against an untraced pipeline.
/// <see cref="Listening"/> toggles whether an <see cref="ActivityListener"/> is attached:
/// <list type="bullet">
/// <item><description><c>Listening=false</c> - nothing is exporting. <c>StartActivity</c> returns
/// <c>null</c>, so the decorator must be genuinely free (it returns the inner task directly, allocating
/// no per-stage async state machine). The traced arm should match the untraced arm here.</description></item>
/// <item><description><c>Listening=true</c> - a listener is attached (as when an OTel exporter is wired),
/// so the full span-per-stage cost is paid. This is the real overhead of always-on per-middleware
/// tracing.</description></item>
/// </list>
/// <see cref="MiddlewareCount"/> is parameterized because the cost scales with stage count (one span per
/// stage).
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class TracingMiddlewareBenchmarks
{
    public sealed class BenchContext;

    private sealed class FakeTransport : ICurrentTransport
    {
        public string Name => "bench";
    }

    private sealed class FakeGetter : IMessageGetter<BenchContext>
    {
        private static readonly ITopic Topic = new Topic("bench-topic", "1.0.0");
        public ITopic GetTopic(BenchContext context) => Topic;
        public string GetBody(BenchContext context) => string.Empty;
        public IDictionary<string, string> GetHeaders(BenchContext context) => new Dictionary<string, string>();
    }

    private sealed class FakeLookUp : IMessageHandlerDefinitionLookUp
    {
        public IMessageHandlerDefinition FindHandler(ITopic topic) => null!;
        public IMessageHandlerDefinition[] GetAllHandlers() => System.Array.Empty<IMessageHandlerDefinition>();
    }

    [Params(3, 8)]
    public int MiddlewareCount { get; set; }

    [Params(false, true)]
    public bool Listening { get; set; }

    private MicrosoftServiceResolverFactory _tracedFactory = null!;
    private MicrosoftServiceResolverFactory _plainFactory = null!;
    private IMiddlewarePipeline<BenchContext> _tracedPipeline = null!;
    private IMiddlewarePipeline<BenchContext> _plainPipeline = null!;
    private ActivityListener _listener;
    private readonly BenchContext _context = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        if (Listening)
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == BenzeneDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        _tracedPipeline = BuildPipeline(traced: true, out _tracedFactory);
        _plainPipeline = BuildPipeline(traced: false, out _plainFactory);
    }

    private IMiddlewarePipeline<BenchContext> BuildPipeline(bool traced, out MicrosoftServiceResolverFactory factory)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentTransport>(new FakeTransport());
        services.AddSingleton<IMessageGetter<BenchContext>>(new FakeGetter());
        services.AddSingleton<IMessageHandlerDefinitionLookUp>(new FakeLookUp());

        var container = new MicrosoftBenzeneServiceContainer(services);
        if (traced)
        {
            container.AddActivityPerMiddleware();
        }

        var builder = new MiddlewarePipelineBuilder<BenchContext>(container);
        for (var i = 0; i < MiddlewareCount; i++)
        {
            builder.Use($"stage{i}", (_, next) => next());
        }

        var pipeline = builder.Build();
        factory = new MicrosoftServiceResolverFactory(services);
        return pipeline;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _listener?.Dispose();
        _tracedFactory.Dispose();
        _plainFactory.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Untraced pipeline, fresh scope per message")]
    public async Task Untraced()
    {
        using var resolver = _plainFactory.CreateScope();
        await _plainPipeline.HandleAsync(_context, resolver);
    }

    [Benchmark(Description = "Activity-traced pipeline, fresh scope per message")]
    public async Task Traced()
    {
        using var resolver = _tracedFactory.CreateScope();
        await _tracedPipeline.HandleAsync(_context, resolver);
    }
}
