using System;
using System.Linq;
using Benzene.Core.Versioning.Deserializer;
using Benzene.Core.Versioning.Schemas;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;
using V3 = Benzene.Test.Core.Versioning.Schemas.V3;
using V4 = Benzene.Test.Core.Versioning.Schemas.V4;
using V5 = Benzene.Test.Core.Versioning.Schemas.V5;

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

    /// <summary>
    /// A service that has been through several schema versions (here V1..V5) doesn't need a direct
    /// caster between every pair it still accepts - the BFS chain composition in
    /// <see cref="SchemaCastDefinitionsExpander"/> already prefers a registered shortcut over a
    /// longer step-by-step chain, because it marks a version visited (and therefore never
    /// reconsiders it) the first time any edge reaches it. Registering a direct V1-&gt;V3 caster
    /// alongside the full V1-&gt;V2-&gt;V3-&gt;V4-&gt;V5 chain must produce the 3-hop
    /// [V1-&gt;V3, V3-&gt;V4, V4-&gt;V5] composition, not the 4-hop step-by-step one - verified by
    /// tagging each candidate route into V3 with a distinct marker value and asserting on which one
    /// survives into the final V1-&gt;V5 result.
    /// </summary>
    [Fact]
    public void Expand_PrefersShortcutCasterOverLongerChain()
    {
        var casters = new SchemaCastersBuilder()
            .Add<V1.OrderPayload, V2.OrderPayload>("orderCreated", "V1", "V2")
            .Add<V2.OrderPayload, V3.OrderPayload>("orderCreated", "V2", "V3", x => x.RegisterInitValue(o => o.Route, "via-v2-longer-chain"))
            .Add<V1.OrderPayload, V3.OrderPayload>("orderCreated", "V1", "V3", x => x.RegisterInitValue(o => o.Route, "via-v1-v3-shortcut"))
            .Add<V3.OrderPayload, V4.OrderPayload>("orderCreated", "V3", "V4")
            .Add<V4.OrderPayload, V5.OrderPayload>("orderCreated", "V4", "V5")
            .Build();

        var versions = new PayloadSchemaVersions
        {
            Topic = "orderCreated",
            FromSchemas = new[] { "V1" },
            ToSchemas = new[] { "V5" }
        };

        var expanded = new SchemaCastDefinitionsExpander().Expand(casters, new[] { versions });

        var composite = (ISchemaCaster<V1.OrderPayload, V5.OrderPayload>)Assert.Single(expanded);

        var result = composite.Caster.Cast(new V1.OrderPayload { Id = "order-1", Quantity = 5 });

        Assert.Equal("order-1", result.Id);
        Assert.Equal(5, result.Quantity);
        Assert.Equal("via-v1-v3-shortcut", result.Route);
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
