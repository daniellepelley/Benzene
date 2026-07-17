using System;
using System.Linq;
using Benzene.Core.Versioning.Deserializer;
using Benzene.Core.Versioning.Schemas;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;
using V3 = Benzene.Test.Core.Versioning.Schemas.V3;

namespace Benzene.Test.Core.Versioning;

public class SchemaCastDefinitionsExpanderTest
{
    [Fact]
    public void Expand_ComposesUpcastChainWhenNoDirectCasterExists()
    {
        var casters = new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("orderCreated", "V1", "V2", x => x.RegisterInitValue(o => o.Currency, "USD"))
            .Add<V2.OrderPayload, V3.OrderPayload>("orderCreated", "V2", "V3", x => x.RegisterInitValue(o => o.Reference, "migrated"))
            .Build();

        var versions = new PayloadSchemaVersions
        {
            Topic = "orderCreated",
            FromSchemas = new[] { "V1", "V2", "V3" },
            ToSchemas = new[] { "V3" }
        };

        var expanded = new SchemaCastDefinitionsExpander().Expand(casters, new[] { versions });

        Assert.Equal(2, expanded.Length);

        var composite = (ISchemaCaster<V1.OrderPayload, V3.OrderPayload>)new SchemaCasters(expanded)
            .GetSchemaCaster("V1", "V3", "orderCreated");

        var result = composite.Caster.Cast(new V1.OrderPayload { Id = "order-1", Quantity = 5 });

        Assert.Equal("order-1", result.Id);
        Assert.Equal(5, result.Quantity);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("migrated", result.Reference);
    }

    [Fact]
    public void Expand_ComposesDowncastChain()
    {
        var casters = new SchemaCastersBuilder()
            .Add<V3.OrderPayload, V2.OrderPayload>("orderCreated", "V3", "V2")
            .Add<V2.OrderPayload, V1.OrderPayload>("orderCreated", "V2", "V1")
            .Build();

        var versions = new PayloadSchemaVersions
        {
            Topic = "orderCreated",
            FromSchemas = new[] { "V3" },
            ToSchemas = new[] { "V1" }
        };

        var expanded = new SchemaCastDefinitionsExpander().Expand(casters, new[] { versions });

        var composite = (ISchemaCaster<V3.OrderPayload, V1.OrderPayload>)Assert.Single(expanded);

        var result = composite.Caster.Cast(new V3.OrderPayload { Id = "order-3", Quantity = 2, Currency = "EUR" });

        Assert.Equal("order-3", result.Id);
        Assert.Equal(2, result.Quantity);
    }

    [Fact]
    public void Expand_ReusesDirectCasterWhenOneExists()
    {
        var casters = new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("orderCreated", "V1", "V2")
            .Build();

        var versions = new PayloadSchemaVersions
        {
            Topic = "orderCreated",
            FromSchemas = new[] { "V1" },
            ToSchemas = new[] { "V2" }
        };

        var expanded = new SchemaCastDefinitionsExpander().Expand(casters, new[] { versions });

        Assert.Same(casters.Single(), Assert.Single(expanded));
    }

    [Fact]
    public void Expand_ThrowsWhenNoCastingPathExists()
    {
        var casters = new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("orderCreated", "V1", "V2")
            .Build();

        var versions = new PayloadSchemaVersions
        {
            Topic = "orderCreated",
            FromSchemas = new[] { "V3" },
            ToSchemas = new[] { "V1" }
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new SchemaCastDefinitionsExpander().Expand(casters, new[] { versions }));

        Assert.Contains("No conversion path found", exception.Message);
    }

    [Fact]
    public void Expand_IgnoresCastersRegisteredForOtherTopics()
    {
        var casters = new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("otherTopic", "V1", "V2")
            .Build();

        var versions = new PayloadSchemaVersions
        {
            Topic = "orderCreated",
            FromSchemas = new[] { "V1" },
            ToSchemas = new[] { "V2" }
        };

        Assert.Throws<InvalidOperationException>(
            () => new SchemaCastDefinitionsExpander().Expand(casters, new[] { versions }));
    }
}
