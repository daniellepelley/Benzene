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

    private enum Colour { Red, Green }
    private class NullabilityFrom { public int Quantity { get; set; } public int? Optional { get; set; } public Colour Status { get; set; } }
    private class NullabilityTo { public int? Quantity { get; set; } public int Optional { get; set; } public Colour? Status { get; set; } }

    [Fact]
    public void Cast_PropertyNullabilityChange_PreservesTheValue()
    {
        // Making a field optional (int->int?) or required (int?->int), and enum<->nullable-enum, are
        // routine schema-evolution moves. They used to fall through to class-mapping and come back
        // `default` (null/0), silently dropping the value. Assert the value survives both directions.
        var caster = new CasterFactory<NullabilityFrom, NullabilityTo>().Build();

        var result = caster.Cast(new NullabilityFrom { Quantity = 5, Optional = 7, Status = Colour.Green });

        Assert.Equal(5, result.Quantity);            // int -> int?
        Assert.Equal(7, result.Optional);            // int? -> int (has value)
        Assert.Equal(Colour.Green, result.Status);   // enum -> nullable enum
    }

    [Fact]
    public void Cast_NullableToNonNullable_WhenNull_UsesDefault_WithoutThrowing()
    {
        var caster = new CasterFactory<NullabilityFrom, NullabilityTo>().Build();

        var result = caster.Cast(new NullabilityFrom { Quantity = 5, Optional = null, Status = Colour.Red });

        Assert.Equal(0, result.Optional);            // int? null -> int : default, not an exception
    }

    private class LineFrom { public string Sku { get; set; } public int Count { get; set; } }
    private class LineTo { public string Sku { get; set; } public int Count { get; set; } }
    private class ArrayFrom { public LineFrom[] Lines { get; set; } }
    private class ArrayTo { public LineTo[] Lines { get; set; } }

    [Fact]
    public void Cast_ArrayPropertyWithChangedElementType_MapsElementWise()
    {
        // An array property whose element type changes between versions used to throw at startup
        // (Expression.New(T[]) has no parameterless ctor); it must map element-wise like List<T> does.
        var caster = new CasterFactory<ArrayFrom, ArrayTo>().Build();

        var result = caster.Cast(new ArrayFrom { Lines = new[] { new LineFrom { Sku = "s1", Count = 2 } } });

        var line = Assert.Single(result.Lines);
        Assert.Equal("s1", line.Sku);
        Assert.Equal(2, line.Count);
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
