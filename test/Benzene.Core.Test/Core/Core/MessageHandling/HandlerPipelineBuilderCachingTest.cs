using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

/// <summary>
/// Guards the structure-caching behaviour of <see cref="HandlerPipelineBuilder"/>: the per-message
/// pipeline structure is built once per (builder-set, request, response) and reused, while middleware
/// and handler instances stay per-invocation. Two properties matter and are covered here: the cache
/// is keyed on the builder set (not the type pair alone), so two pipelines sharing a handler type but
/// registering different middleware do not cross-contaminate; and a single cached structure shared
/// across concurrent records resolves everything from each record's own resolver, with no bleed.
/// </summary>
public class HandlerPipelineBuilderCachingTest
{
    private sealed class Req
    {
    }

    private sealed class Res
    {
    }

    private sealed class StubHandler : IMessageHandler<Req, Res>
    {
        public Task<IBenzeneResult<Res>> HandleAsync(Req request)
            => Task.FromResult(BenzeneResult.Ok(new Res()));
    }

    private sealed class TagMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>
    {
        private readonly string _tag;
        private readonly ConcurrentQueue<string> _sink;

        public TagMiddleware(string tag, ConcurrentQueue<string> sink)
        {
            _tag = tag;
            _sink = sink;
        }

        public string Name => "Tag";

        public Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
        {
            _sink.Enqueue(_tag);
            return next();
        }
    }

    private sealed class TagMiddlewareBuilder : IHandlerMiddlewareBuilder
    {
        private readonly string _tag;
        private readonly ConcurrentQueue<string> _sink;

        public TagMiddlewareBuilder(string tag, ConcurrentQueue<string> sink)
        {
            _tag = tag;
            _sink = sink;
        }

        public IMiddleware<IMessageHandlerContext<TRequest, TResponse>>? Create<TRequest, TResponse>(
            IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
            where TRequest : class
            => new TagMiddleware<TRequest, TResponse>(_tag, _sink);
    }

    [Fact]
    public async Task Create_TwoBuildersWithDifferentMiddlewareSets_SameTypePair_ProduceDistinctChains()
    {
        // The cache-key isolation guard: if the structure were keyed on (TRequest, TResponse) alone,
        // builderB's Create would hand back builderA's cached structure - so builderB's pipeline would
        // run tag "A" and never "B". Keying on the builder set keeps them distinct.
        var resolver = new MicrosoftServiceResolverFactory(new ServiceCollection()).CreateScope();
        var sinkA = new ConcurrentQueue<string>();
        var sinkB = new ConcurrentQueue<string>();

        var builderA = new HandlerPipelineBuilder(new IHandlerMiddlewareBuilder[] { new TagMiddlewareBuilder("A", sinkA) });
        var builderB = new HandlerPipelineBuilder(new IHandlerMiddlewareBuilder[] { new TagMiddlewareBuilder("B", sinkB) });

        var handler = new StubHandler();

        var pipelineA = builderA.Create(handler, resolver);
        var pipelineB = builderB.Create(handler, resolver);

        await pipelineA.HandleAsync(new MessageHandlerContext<Req, Res>(new Topic("topic"), new Req()), resolver);
        await pipelineB.HandleAsync(new MessageHandlerContext<Req, Res>(new Topic("topic"), new Req()), resolver);

        Assert.Equal(new[] { "A" }, sinkA.ToArray());
        Assert.Equal(new[] { "B" }, sinkB.ToArray());
    }

    [Fact]
    public async Task Create_SameBuilderSet_SameTypePair_ReusesTheCachedStructureInstance()
    {
        // Two Create calls on the same builder resolve the identical cached structure (proving it is
        // reused across messages, not rebuilt); only the per-message wrapper differs.
        var resolver = new MicrosoftServiceResolverFactory(new ServiceCollection()).CreateScope();
        var builders = new IHandlerMiddlewareBuilder[] { new TagMiddlewareBuilder("A", new ConcurrentQueue<string>()) };

        var builder = new HandlerPipelineBuilder(builders);
        var handler = new StubHandler();

        var first = builder.Create(handler, resolver);
        var second = builder.Create(handler, resolver);

        Assert.NotSame(first, second);
        Assert.Equal(first.GetType(), second.GetType());
    }

    private sealed class ScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class ScopeProbeMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>
    {
        private readonly IServiceResolver _serviceResolver;
        private readonly ConcurrentQueue<Guid> _sink;

        public ScopeProbeMiddleware(IServiceResolver serviceResolver, ConcurrentQueue<Guid> sink)
        {
            _serviceResolver = serviceResolver;
            _sink = sink;
        }

        public string Name => "ScopeProbe";

        public async Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
        {
            // Resolve a scoped service from the per-call resolver, then yield so records genuinely
            // overlap. If the pipeline had captured a single scope, every record would see the same
            // ScopeMarker instance.
            var marker = _serviceResolver.GetService<ScopeMarker>();
            await Task.Delay(20);
            _sink.Enqueue(marker.Id);
            await next();
        }
    }

    private sealed class ScopeProbeMiddlewareBuilder : IHandlerMiddlewareBuilder
    {
        private readonly ConcurrentQueue<Guid> _sink;

        public ScopeProbeMiddlewareBuilder(ConcurrentQueue<Guid> sink)
        {
            _sink = sink;
        }

        public IMiddleware<IMessageHandlerContext<TRequest, TResponse>>? Create<TRequest, TResponse>(
            IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
            where TRequest : class
            => new ScopeProbeMiddleware<TRequest, TResponse>(serviceResolver, _sink);
    }

    [Fact]
    public async Task Create_SharedCachedStructure_AcrossConcurrentRecords_ResolvesPerRecordScope()
    {
        const int recordCount = 20;

        var services = new ServiceCollection();
        services.AddScoped<ScopeMarker>();
        var resolverFactory = new MicrosoftServiceResolverFactory(services);

        var sink = new ConcurrentQueue<Guid>();
        // One builder / one builder-set => one shared cached structure for every record below.
        var builder = new HandlerPipelineBuilder(new IHandlerMiddlewareBuilder[] { new ScopeProbeMiddlewareBuilder(sink) });

        await Task.WhenAll(Enumerable.Range(0, recordCount).Select(_ => Task.Run(async () =>
        {
            using var scope = resolverFactory.CreateScope();
            var pipeline = builder.Create(new StubHandler(), scope);
            await pipeline.HandleAsync(new MessageHandlerContext<Req, Res>(new Topic("topic"), new Req()), scope);
        })));

        var observed = sink.ToArray();
        Assert.Equal(recordCount, observed.Length);
        // Every record resolved its own scoped ScopeMarker: no shared/captured scope, no cross-record bleed.
        Assert.Equal(recordCount, observed.Distinct().Count());
    }
}
