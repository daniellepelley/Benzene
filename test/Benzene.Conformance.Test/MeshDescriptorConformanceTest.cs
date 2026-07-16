using System.Text.Json;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Conformance.Test.Handlers;
using Benzene.Core.MessageHandlers;
using Benzene.Mesh.Wire;
using Xunit;

namespace Benzene.Conformance.Test;

/// <summary>
/// Runs docs/specification/conformance/mesh-descriptor-cases.json: derives the ServiceDescriptor
/// (mesh.md §2) from the canonical conformance handlers and asserts the derived payload schemas
/// plus the descriptorHash's format/invariance/sensitivity properties. runtime and the hash value
/// are per-port by design and not pinned by the fixture.
/// </summary>
public class MeshDescriptorConformanceTest
{
    public class DescriptorFixture
    {
        public ServiceInfoSection ServiceInfo { get; set; } = new();
        public JsonElement ExpectedDescriptor { get; set; }
        public HashSection Hash { get; set; } = new();
    }

    public class ServiceInfoSection
    {
        public string Service { get; set; } = string.Empty;
        public string? ServiceVersion { get; set; }
        public PlacementSection Placement { get; set; } = new();
    }

    public class PlacementSection
    {
        public string Cloud { get; set; } = string.Empty;
        public string? Region { get; set; }
    }

    public class HashSection
    {
        public string Prefix { get; set; } = string.Empty;
        public int HexLength { get; set; }
        public bool InvariantToInstanceId { get; set; }
        public bool SensitiveToServiceVersion { get; set; }
        public bool SensitiveToTopics { get; set; }
    }

    private static readonly Lazy<DescriptorFixture> Fixture = new(() =>
        ConformanceFixtures.Load<DescriptorFixture>("mesh-descriptor-cases.json"));

    private static MeshServiceInfo Info(string? instanceId = null, string? serviceVersion = null)
    {
        var fixture = Fixture.Value;
        return new MeshServiceInfo(
            fixture.ServiceInfo.Service,
            serviceVersion ?? fixture.ServiceInfo.ServiceVersion,
            instanceId,
            placement: new MeshPlacement
            {
                Cloud = fixture.ServiceInfo.Placement.Cloud,
                Region = fixture.ServiceInfo.Placement.Region
            });
    }

    private static IMessageHandlerDefinitionLookUp CanonicalLookUp(params Type[] extraHandlerTypes)
    {
        var types = new[] { typeof(GreetConformanceHandler), typeof(StatusConformanceHandler) }
            .Concat(extraHandlerTypes)
            .ToArray();
        return new DefinitionsLookUp(new ReflectionMessageHandlersFinder(types).FindDefinitions());
    }

    [Fact]
    public void DerivedDescriptor_MatchesTheExpectedDescriptor()
    {
        var descriptor = MeshDescriptorFactory.Create(CanonicalLookUp(), Info());

        using var actual = JsonDocument.Parse(MeshJson.Serialize(descriptor));
        var mismatch = ConformanceFixtures.FindSubsetMismatch(Fixture.Value.ExpectedDescriptor, actual.RootElement);
        Assert.True(mismatch == null, $"descriptor mismatch at {mismatch}");
    }

    [Fact]
    public void DescriptorHash_HasTheWireFormat()
    {
        var hash = MeshDescriptorFactory.Create(CanonicalLookUp(), Info()).DescriptorHash;

        Assert.NotNull(hash);
        Assert.StartsWith(Fixture.Value.Hash.Prefix, hash);
        Assert.Equal(Fixture.Value.Hash.Prefix.Length + Fixture.Value.Hash.HexLength, hash!.Length);
        Assert.Matches("^[0-9a-f]+$", hash.Substring(Fixture.Value.Hash.Prefix.Length));
    }

    [Fact]
    public void DescriptorHash_IsInvariantToInstanceId()
    {
        if (!Fixture.Value.Hash.InvariantToInstanceId) return; // not asserted by the fixture

        var first = MeshDescriptorFactory.Create(CanonicalLookUp(), Info(instanceId: "instance-1"));
        var second = MeshDescriptorFactory.Create(CanonicalLookUp(), Info(instanceId: "instance-2"));

        Assert.Equal(first.DescriptorHash, second.DescriptorHash);
    }

    [Fact]
    public void DescriptorHash_IsSensitiveToServiceVersion()
    {
        if (!Fixture.Value.Hash.SensitiveToServiceVersion) return; // not asserted by the fixture

        var baseline = MeshDescriptorFactory.Create(CanonicalLookUp(), Info());
        var bumped = MeshDescriptorFactory.Create(CanonicalLookUp(),
            Info(serviceVersion: Fixture.Value.ServiceInfo.ServiceVersion + "-changed"));

        Assert.NotEqual(baseline.DescriptorHash, bumped.DescriptorHash);
    }

    [Fact]
    public void DescriptorHash_IsSensitiveToTheTopicSet()
    {
        if (!Fixture.Value.Hash.SensitiveToTopics) return; // not asserted by the fixture

        var baseline = MeshDescriptorFactory.Create(CanonicalLookUp(), Info());
        var grown = MeshDescriptorFactory.Create(CanonicalLookUp(typeof(PanicConformanceHandler)), Info());

        Assert.NotEqual(baseline.DescriptorHash, grown.DescriptorHash);
    }

    [Fact]
    public void MissingRegistry_DegradesTheFeedNotTheDescriptor()
    {
        var descriptor = MeshDescriptorFactory.Create(null, Info());

        Assert.Empty(descriptor.Topics);
        Assert.Equal(new List<string> { MeshDescriptorFactory.RegistryFeed }, descriptor.Degraded);
        Assert.Equal(Fixture.Value.ServiceInfo.Service, descriptor.Service);
        Assert.NotNull(descriptor.DescriptorHash);
    }

    private class DefinitionsLookUp : IMessageHandlerDefinitionLookUp
    {
        private readonly IMessageHandlerDefinition[] _definitions;

        public DefinitionsLookUp(IMessageHandlerDefinition[] definitions)
        {
            _definitions = definitions;
        }

        public IMessageHandlerDefinition? FindHandler(ITopic topic)
        {
            return _definitions.FirstOrDefault(x => x.Topic.Id == topic.Id && x.Topic.Version == topic.Version);
        }

        public IMessageHandlerDefinition[] GetAllHandlers()
        {
            return _definitions;
        }
    }
}
