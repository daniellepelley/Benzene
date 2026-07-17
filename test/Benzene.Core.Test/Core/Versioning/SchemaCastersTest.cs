using System;
using Benzene.Core.Versioning.Schemas;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;

namespace Benzene.Test.Core.Versioning;

public class SchemaCastersTest
{
    private static ISchemaCaster[] BuildCasters()
    {
        return new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("orderCreated", "V1", "V2")
            .Add<V2.OrderPayload, V1.OrderPayload>("orderCreated", "V2", "V1")
            .Build();
    }

    [Fact]
    public void GetSchemaCaster_FindsMatchingDefinition()
    {
        var casters = new SchemaCasters(BuildCasters());

        var caster = casters.GetSchemaCaster("V1", "V2", "orderCreated");

        Assert.Equal("V1", caster.Definition.FromSchema);
        Assert.Equal("V2", caster.Definition.ToSchema);
        Assert.Equal("orderCreated", caster.Definition.Topic);
        Assert.Equal(typeof(V1.OrderPayload), caster.FromType);
        Assert.Equal(typeof(V2.OrderPayload), caster.ToType);
    }

    [Fact]
    public void GetSchemaCaster_ThrowsWhenNoDefinitionMatches()
    {
        var casters = new SchemaCasters(BuildCasters());

        var exception = Assert.Throws<InvalidOperationException>(
            () => casters.GetSchemaCaster("V1", "V2", "unknownTopic"));

        Assert.Contains("unknownTopic", exception.Message);
    }

    [Fact]
    public void SchemaCasterBuilder_BuiltCaster_CastsPayloads()
    {
        var casters = new SchemaCasters(BuildCasters());

        var caster = (ISchemaCaster<V1.OrderPayload, V2.OrderPayload>)casters.GetSchemaCaster("V1", "V2", "orderCreated");

        var result = caster.Caster.Cast(new V1.OrderPayload { Id = "order-1", Quantity = 5 });

        Assert.Equal("order-1", result.Id);
        Assert.Equal(5, result.Quantity);
    }

    [Fact]
    public void SchemaCaster_CastNonGeneric_InvokesTheTypedCaster()
    {
        var caster = new SchemaCasters(BuildCasters()).GetSchemaCaster("V1", "V2", "orderCreated");

        var result = (V2.OrderPayload)caster.Cast(new V1.OrderPayload { Id = "order-1", Quantity = 5 });

        Assert.Equal("order-1", result.Id);
        Assert.Equal(5, result.Quantity);
    }

    [Fact]
    public void TryGetSchemaCaster_ByFromSchemaAndToType_FindsTheRequestUpcastCaster()
    {
        var casters = new SchemaCasters(BuildCasters());

        var found = casters.TryGetSchemaCaster("orderCreated", "V1", typeof(V2.OrderPayload), out var caster);

        Assert.True(found);
        Assert.Equal(typeof(V1.OrderPayload), caster.FromType);
        Assert.Equal(typeof(V2.OrderPayload), caster.ToType);
    }

    [Fact]
    public void TryGetSchemaCaster_ByFromTypeAndToSchema_FindsTheResponseDowncastCaster()
    {
        var casters = new SchemaCasters(BuildCasters());

        var found = casters.TryGetSchemaCaster("orderCreated", typeof(V2.OrderPayload), "V1", out var caster);

        Assert.True(found);
        Assert.Equal(typeof(V2.OrderPayload), caster.FromType);
        Assert.Equal(typeof(V1.OrderPayload), caster.ToType);
    }

    [Fact]
    public void TryGetSchemaCaster_NoMatch_ReturnsFalse()
    {
        var casters = new SchemaCasters(BuildCasters());

        Assert.False(casters.TryGetSchemaCaster("orderCreated", "V9", typeof(V2.OrderPayload), out _));
        Assert.False(casters.TryGetSchemaCaster("otherTopic", "V1", typeof(V2.OrderPayload), out _));
        Assert.False(casters.TryGetSchemaCaster("orderCreated", typeof(V2.OrderPayload), "V9", out _));
    }
}
