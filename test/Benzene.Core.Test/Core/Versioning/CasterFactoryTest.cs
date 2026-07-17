using System.Collections.Generic;
using Benzene.Core.Versioning.CasterBuilder;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;

namespace Benzene.Test.Core.Versioning;

public class CasterFactoryTest
{
    private static V1.OrderPayload CreateV1Order()
    {
        return new V1.OrderPayload
        {
            Id = "order-1",
            Quantity = 3,
            CustomerName = "Jo Bloggs",
            Discount = 1.5m,
            Status = V1.OrderStatus.Shipped,
            ShippingAddress = new V1.Address { City = "London", PostCode = "N1 1AA" },
            Lines = new List<V1.OrderLine>
            {
                new V1.OrderLine { Sku = "sku-1", Count = 1 },
                new V1.OrderLine { Sku = "sku-2", Count = 2 }
            }
        };
    }

    [Fact]
    public void Cast_MapsMatchingProperties()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>().Build();

        var result = caster.Cast(CreateV1Order());

        Assert.Equal("order-1", result.Id);
        Assert.Equal(3, result.Quantity);
        Assert.Equal(1.5m, result.Discount);
    }

    [Fact]
    public void Cast_MapsEnumsByValue()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>().Build();

        var result = caster.Cast(CreateV1Order());

        Assert.Equal(V2.OrderStatus.Shipped, result.Status);
    }

    [Fact]
    public void Cast_MapsNestedClassesByName()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>().Build();

        var result = caster.Cast(CreateV1Order());

        Assert.Equal("London", result.ShippingAddress.City);
        Assert.Equal("N1 1AA", result.ShippingAddress.PostCode);
    }

    [Fact]
    public void Cast_MapsNullNestedClassToNull()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>().Build();

        var from = CreateV1Order();
        from.ShippingAddress = null;

        var result = caster.Cast(from);

        Assert.Null(result.ShippingAddress);
    }

    [Fact]
    public void Cast_MapsListsOfDifferentElementTypes()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>().Build();

        var result = caster.Cast(CreateV1Order());

        Assert.Equal(2, result.Lines.Count);
        Assert.Equal("sku-1", result.Lines[0].Sku);
        Assert.Equal(1, result.Lines[0].Count);
        Assert.Equal("sku-2", result.Lines[1].Sku);
        Assert.Equal(2, result.Lines[1].Count);
    }

    [Fact]
    public void Cast_MapsNullListToNull()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>().Build();

        var from = CreateV1Order();
        from.Lines = null;

        var result = caster.Cast(from);

        Assert.Null(result.Lines);
    }

    [Fact]
    public void Cast_MapsRootLevelLists()
    {
        var caster = new CasterFactory<List<V1.OrderLine>, List<V2.OrderLine>>().Build();

        var result = caster.Cast(new List<V1.OrderLine> { new V1.OrderLine { Sku = "sku-9", Count = 9 } });

        var line = Assert.Single(result);
        Assert.Equal("sku-9", line.Sku);
        Assert.Equal(9, line.Count);
    }

    [Fact]
    public void Cast_NewPropertyDefaultsWhenNoInitValue()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>().Build();

        var result = caster.Cast(CreateV1Order());

        Assert.Null(result.Currency);
    }

    [Fact]
    public void Cast_RegisterInitValue_SetsConstantOnNewProperty()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>()
            .RegisterInitValue(x => x.Currency, "USD")
            .Build();

        var result = caster.Cast(CreateV1Order());

        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Cast_RegisterInitValue_SetsFuncValueOnNewProperty()
    {
        var count = 0;
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>()
            .RegisterInitValue(x => x.Currency, () => $"GBP-{++count}")
            .Build();

        Assert.Equal("GBP-1", caster.Cast(CreateV1Order()).Currency);
        Assert.Equal("GBP-2", caster.Cast(CreateV1Order()).Currency);
    }

    [Fact]
    public void Cast_RegisterInitValue_OverridesMappedProperty()
    {
        var caster = new CasterFactory<V1.OrderPayload, V2.OrderPayload>()
            .RegisterInitValue(x => x.Quantity, 42)
            .Build();

        var result = caster.Cast(CreateV1Order());

        Assert.Equal(42, result.Quantity);
    }

    [Fact]
    public void Cast_DowncastDropsPropertiesRemovedFromTarget()
    {
        var caster = new CasterFactory<V2.OrderPayload, V1.OrderPayload>().Build();

        var result = caster.Cast(new V2.OrderPayload
        {
            Id = "order-2",
            Quantity = 7,
            Currency = "EUR",
            Status = V2.OrderStatus.Pending
        });

        Assert.Equal("order-2", result.Id);
        Assert.Equal(7, result.Quantity);
        Assert.Equal(V1.OrderStatus.Pending, result.Status);
        Assert.Null(result.CustomerName);
    }

    [Fact]
    public void Cast_MapsPolymorphicPropertyByMatchingDerivedTypeName()
    {
        var caster = new CasterFactory<V1.Drawing, V2.Drawing>().Build();

        var result = caster.Cast(new V1.Drawing
        {
            Name = "picture",
            Shape = new V1.Circle { Colour = "red", Radius = 4.5 }
        });

        Assert.Equal("picture", result.Name);
        var circle = Assert.IsType<V2.Circle>(result.Shape);
        Assert.Equal("red", circle.Colour);
        Assert.Equal(4.5, circle.Radius);
    }

    [Fact]
    public void Cast_RegisterTypeMapping_MapsRenamedDerivedType()
    {
        var caster = new CasterFactory<V1.Drawing, V2.Drawing>()
            .RegisterTypeMapping<V1.Oval, V2.Ellipse>()
            .Build();

        var result = caster.Cast(new V1.Drawing
        {
            Name = "picture",
            Shape = new V1.Oval { Colour = "blue", Width = 2, Height = 3 }
        });

        var ellipse = Assert.IsType<V2.Ellipse>(result.Shape);
        Assert.Equal("blue", ellipse.Colour);
        Assert.Equal(2, ellipse.Width);
        Assert.Equal(3, ellipse.Height);
    }
}
