using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Benchmarks;

/// <summary>
/// Benchmarks the per-message handler-pipeline build path:
/// <see cref="HandlerPipelineBuilder.Create{TRequest,TResponse}"/>, which
/// <c>PipelineMessageHandlerWrapper.Wrap</c> (and therefore
/// <c>MessageHandlerFactory.Create</c>) invokes on <em>every</em> dispatched message. Each call
/// allocates from scratch: a <c>List</c>, one middleware instance per registered
/// <see cref="IHandlerMiddlewareBuilder"/>, a <c>.Select(...).ToArray()</c> projection into a
/// <c>Func[]</c>, and a new <c>MiddlewarePipeline</c> whose constructor reverses the array again -
/// even though the pipeline <em>structure</em> is fixed after startup (only the middleware
/// <em>instances</em> are genuinely per-scope). The top-level <c>MiddlewarePipeline</c> deliberately
/// splits "structure once, instances per request"; this path does not, nullifying that optimization
/// here.
/// <para>
/// This is the exact allocation hotspot flagged as the heaviest remaining per-message cost on the
/// core dispatch path. It is isolated at the public <see cref="HandlerPipelineBuilder"/> surface
/// (the internal <c>MessageHandlerFactory</c> wrapper adds only a memoized logger lookup and one
/// <c>Topic</c> alloc on top) so a structure-caching fix can be <em>proven</em>: allocated bytes/op
/// should drop as a step that grows with <see cref="HandlerMiddlewareCount"/>, not merely asserted.
/// A long-lived resolver is reused so this isolates build/invoke cost from DI-scope-creation cost
/// (which <see cref="MiddlewarePipelineBenchmarks"/> covers separately).
/// </para>
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class HandlerCreationBenchmarks
{
    public sealed class BenchRequest
    {
        public string Name { get; init; } = "bench";
    }

    public sealed class BenchResponse
    {
        public string Result { get; init; } = "ok";
    }

    private sealed class BenchHandler : IMessageHandler<BenchRequest, BenchResponse>
    {
        private static readonly IBenzeneResult<BenchResponse> Response =
            BenzeneResult.Ok(new BenchResponse());

        public Task<IBenzeneResult<BenchResponse>> HandleAsync(BenchRequest request)
            => Task.FromResult(Response);
    }

    /// <summary>A pass-through handler middleware, one instance created per build today.</summary>
    private sealed class PassThroughMiddleware<TRequest, TResponse>
        : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>
    {
        public string Name => nameof(PassThroughMiddleware<TRequest, TResponse>);

        public Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
            => next();
    }

    /// <summary>Contributes one <see cref="PassThroughMiddleware{TRequest,TResponse}"/> to every pipeline built.</summary>
    private sealed class PassThroughMiddlewareBuilder : IHandlerMiddlewareBuilder
    {
        public IMiddleware<IMessageHandlerContext<TRequest, TResponse>>? Create<TRequest, TResponse>(
            IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
            where TRequest : class
            => new PassThroughMiddleware<TRequest, TResponse>();
    }

    [Params(0, 1, 3)]
    public int HandlerMiddlewareCount { get; set; }

    private MicrosoftServiceResolverFactory _resolverFactory = null!;
    private IServiceResolver _resolver = null!;
    private HandlerPipelineBuilder _pipelineBuilder = null!;
    private BenchHandler _handler = null!;
    private ITopic _topic = null!;
    private BenchRequest _request = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        _resolverFactory = new MicrosoftServiceResolverFactory(services);
        _resolver = _resolverFactory.CreateScope();

        var builders = Enumerable.Range(0, HandlerMiddlewareCount)
            .Select(_ => (IHandlerMiddlewareBuilder)new PassThroughMiddlewareBuilder())
            .ToArray();

        _pipelineBuilder = new HandlerPipelineBuilder(builders);
        _handler = new BenchHandler();
        _topic = new Topic("orders:create", "v1");
        _request = new BenchRequest();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _resolver.Dispose();
        _resolverFactory.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Build the handler pipeline (the per-message rebuild)")]
    public IMiddlewarePipeline<IMessageHandlerContext<BenchRequest, BenchResponse>> BuildPipeline()
        => _pipelineBuilder.Create(_handler, _resolver);

    [Benchmark(Description = "Build + invoke: rebuild the pipeline then run one message through it")]
    public Task<IBenzeneResult<BenchResponse>> BuildAndInvoke()
    {
        var pipeline = _pipelineBuilder.Create(_handler, _resolver);
        var handler = new PipelineMessageHandler<BenchRequest, BenchResponse>(
            _topic, pipeline, _resolver, typeof(BenchHandler));
        return handler.HandleAsync(_request);
    }
}
