using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using BenchmarkDotNet.Attributes;

namespace Benzene.Benchmarks;

/// <summary>
/// Benchmarks the per-message routing hot path: <see cref="MessageHandlerDefinitionLookUp.FindHandler"/>
/// resolving a topic (id + version) to a handler definition against the shared, pre-built
/// <see cref="MessageHandlerDefinitionIndex"/>. <see cref="VersionsPerTopic"/> is parameterized
/// because that's the dimension the version-selection cost scales with: the lookup picks one of N
/// registered versions for the topic id, and folding version selection into the per-candidate
/// predicate made that selection (and its candidate-version array allocation) O(n^2) in N. This
/// suite makes that cost visible and guards it from regressing.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class HandlerRoutingBenchmarks
{
    private sealed class FakeDefinition : IMessageHandlerDefinition
    {
        public ITopic Topic { get; init; } = null!;
        public Type RequestType => typeof(object);
        public Type ResponseType => typeof(object);
        public Type HandlerType => typeof(object);
    }

    private sealed class FakeFinder : IMessageHandlersFinder
    {
        private readonly IMessageHandlerDefinition[] _definitions;

        public FakeFinder(IMessageHandlerDefinition[] definitions) => _definitions = definitions;

        public IMessageHandlerDefinition[] FindDefinitions() => _definitions;
    }

    private const string TopicId = "orders:create";

    [Params(1, 5, 20)]
    public int VersionsPerTopic { get; set; }

    private MessageHandlerDefinitionLookUp _lookUp = null!;
    private ITopic _firstVersionTopic = null!;
    private ITopic _lastVersionTopic = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var definitions = Enumerable.Range(0, VersionsPerTopic)
            .Select(i => (IMessageHandlerDefinition)new FakeDefinition { Topic = new Topic(TopicId, $"v{i}") })
            .ToArray();

        var index = new MessageHandlerDefinitionIndex(new IMessageHandlersFinder[] { new FakeFinder(definitions) });
        _lookUp = new MessageHandlerDefinitionLookUp(index, new VersionSelector());

        // Warm the singleton index build once so the measured calls hit the steady-state lookup, not
        // the one-time aggregation.
        _lookUp.FindHandler(new Topic(TopicId, "v0"));

        _firstVersionTopic = new Topic(TopicId, "v0");
        _lastVersionTopic = new Topic(TopicId, $"v{VersionsPerTopic - 1}");
    }

    [Benchmark(Description = "FindHandler: requested version is the first registered")]
    public IMessageHandlerDefinition FindHandler_FirstVersion() => _lookUp.FindHandler(_firstVersionTopic);

    [Benchmark(Description = "FindHandler: requested version is the last registered")]
    public IMessageHandlerDefinition FindHandler_LastVersion() => _lookUp.FindHandler(_lastVersionTopic);
}
