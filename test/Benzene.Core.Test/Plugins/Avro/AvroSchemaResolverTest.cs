using System.Linq;
using Avro;
using Benzene.Avro;
using Xunit;

namespace Benzene.Test.Plugins.Avro;

public class AvroSchemaResolverTest
{
    [Fact]
    public void Reflection_GeneratesValidRecordSchema_WithAllProperties()
    {
        var resolver = new AvroSchemaResolver(new AvroOptions());

        var schema = resolver.GetSchema(typeof(SampleOrderDto));

        var record = Assert.IsType<RecordSchema>(schema);
        var fieldNames = record.Fields.Select(f => f.Name).ToList();

        Assert.Contains("Name", fieldNames);
        Assert.Contains("Quantity", fieldNames);
        Assert.Contains("Price", fieldNames);
        Assert.Contains("Tags", fieldNames);
        Assert.Contains("OptionalCount", fieldNames);
        Assert.Contains("Leg", fieldNames);
    }

    [Fact]
    public void Reflection_MapsCollectionToArray()
    {
        var resolver = new AvroSchemaResolver(new AvroOptions());

        var record = (RecordSchema)resolver.GetSchema(typeof(SampleOrderDto));
        var tags = record.Fields.First(f => f.Name == "Tags");

        Assert.Equal(global::Avro.Schema.Type.Array, tags.Schema.Tag);
    }

    [Fact]
    public void Reflection_MapsNullableToUnionWithNull()
    {
        var resolver = new AvroSchemaResolver(new AvroOptions());

        var record = (RecordSchema)resolver.GetSchema(typeof(SampleOrderDto));
        var optional = record.Fields.First(f => f.Name == "OptionalCount");

        var union = Assert.IsType<UnionSchema>(optional.Schema);
        Assert.Contains(union.Schemas, s => s.Tag == global::Avro.Schema.Type.Null);
    }

    [Fact]
    public void ExplicitSchema_TakesPrecedenceOverReflection()
    {
        const string pointSchema =
            "{\"type\":\"record\",\"name\":\"CustomPoint\",\"fields\":[" +
            "{\"name\":\"X\",\"type\":\"int\"},{\"name\":\"Y\",\"type\":\"int\"}]}";

        var resolver = new AvroSchemaResolver(new AvroOptions().RegisterSchema<Point>(pointSchema));

        var record = (RecordSchema)resolver.GetSchema(typeof(Point));

        Assert.Equal("CustomPoint", record.Name);
    }
}
