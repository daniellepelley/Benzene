using System;
using System.Collections.Generic;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Core.Versioning.Request;
using Benzene.Core.Versioning.Schemas;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;

namespace Benzene.Test.Core.Versioning;

public class CastingRequestMapperTest
{
    public class TestContext
    {
    }

    private class FakeRequestMapper : IRequestMapper<TestContext>
    {
        private readonly Func<Type, object?> _bodyFor;
        public readonly List<Type> RequestedTypes = new();

        public FakeRequestMapper(Func<Type, object?> bodyFor)
        {
            _bodyFor = bodyFor;
        }

        public TRequest? GetBody<TRequest>(TestContext context) where TRequest : class
        {
            RequestedTypes.Add(typeof(TRequest));
            return (TRequest?)_bodyFor(typeof(TRequest));
        }
    }

    private class FakeVersionGetter : IMessageVersionGetter<TestContext>
    {
        private readonly string? _version;
        public FakeVersionGetter(string? version) => _version = version;
        public string? GetVersion(TestContext context) => _version;
    }

    private class FakeTopicGetter : IMessageTopicGetter<TestContext>
    {
        private readonly string? _topic;
        public FakeTopicGetter(string? topic) => _topic = topic;
        public ITopic? GetTopic(TestContext context) => _topic == null ? null : new Topic(_topic);
    }

    private static ISchemaCasters Casters() =>
        new SchemaCasters(new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("order", "V1", "V2")
            .Build());

    [Fact]
    public void GetBody_VersionSignalledAndCasterRegistered_UpcastsFromIncomingVersion()
    {
        var inner = new FakeRequestMapper(t =>
            t == typeof(V1.OrderPayload) ? new V1.OrderPayload { Id = "order-1", Quantity = 5 } : null);

        var mapper = new CastingRequestMapper<TestContext>(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), Casters());

        var result = mapper.GetBody<V2.OrderPayload>(new TestContext());

        Assert.NotNull(result);
        Assert.Equal("order-1", result.Id);
        Assert.Equal(5, result.Quantity);
        // The inner mapper was asked for the INCOMING version's type (V1), not the handler's V2.
        Assert.Contains(typeof(V1.OrderPayload), inner.RequestedTypes);
        Assert.DoesNotContain(typeof(V2.OrderPayload), inner.RequestedTypes);
    }

    [Fact]
    public void GetBody_NoSchemaCasters_DelegatesToInnerForTargetType()
    {
        var inner = new FakeRequestMapper(_ => new V2.OrderPayload { Id = "order-2" });

        var mapper = new CastingRequestMapper<TestContext>(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), schemaCasters: null);

        var result = mapper.GetBody<V2.OrderPayload>(new TestContext());

        Assert.Equal("order-2", result.Id);
        Assert.Equal(new[] { typeof(V2.OrderPayload) }, inner.RequestedTypes);
    }

    [Fact]
    public void GetBody_NoVersionSignalled_DelegatesToInnerForTargetType()
    {
        var inner = new FakeRequestMapper(_ => new V2.OrderPayload { Id = "order-2" });

        var mapper = new CastingRequestMapper<TestContext>(inner, new FakeVersionGetter(null), new FakeTopicGetter("order"), Casters());

        var result = mapper.GetBody<V2.OrderPayload>(new TestContext());

        Assert.Equal("order-2", result.Id);
        Assert.Equal(new[] { typeof(V2.OrderPayload) }, inner.RequestedTypes);
    }

    [Fact]
    public void GetBody_NoCasterForThisVersionPair_DelegatesToInnerForTargetType()
    {
        var inner = new FakeRequestMapper(_ => new V2.OrderPayload { Id = "order-2" });

        // Version V9 has no registered caster into V2.
        var mapper = new CastingRequestMapper<TestContext>(inner, new FakeVersionGetter("V9"), new FakeTopicGetter("order"), Casters());

        var result = mapper.GetBody<V2.OrderPayload>(new TestContext());

        Assert.Equal("order-2", result.Id);
        Assert.Equal(new[] { typeof(V2.OrderPayload) }, inner.RequestedTypes);
    }

    [Fact]
    public void GetBody_NoTopic_DelegatesToInnerForTargetType()
    {
        var inner = new FakeRequestMapper(_ => new V2.OrderPayload { Id = "order-2" });

        var mapper = new CastingRequestMapper<TestContext>(inner, new FakeVersionGetter("V1"), new FakeTopicGetter(null), Casters());

        var result = mapper.GetBody<V2.OrderPayload>(new TestContext());

        Assert.Equal("order-2", result.Id);
        Assert.Equal(new[] { typeof(V2.OrderPayload) }, inner.RequestedTypes);
    }

    [Fact]
    public void GetBody_InnerReturnsNullForIncomingType_ReturnsNull()
    {
        var inner = new FakeRequestMapper(_ => null);

        var mapper = new CastingRequestMapper<TestContext>(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), Casters());

        Assert.Null(mapper.GetBody<V2.OrderPayload>(new TestContext()));
    }
}
