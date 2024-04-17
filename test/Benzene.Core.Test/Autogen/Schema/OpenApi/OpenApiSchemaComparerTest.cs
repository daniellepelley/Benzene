using Benzene.Schema.OpenApi;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class OpenApiSchemaComparerTest
{
    [Fact]
    public void SchemasMatch()
    {
        var schemaBuilder = new SchemaBuilder();

        schemaBuilder.AddSchema(typeof(Example));
        schemaBuilder.AddSchema(typeof(Inner[]));

        var schemas = schemaBuilder.Build();
        
        var example = schemas["Example"];

        var result = new OpenApiSchemaComparer().Compare(example, example);

        Assert.Empty(result);
    }
    
    [Fact]
    public void SchemasDoNotMatch()
    {
        var schemaBuilder = new SchemaBuilder();

        schemaBuilder.AddSchema(typeof(Example));
        schemaBuilder.AddSchema(typeof(Inner[]));

        var schemas = schemaBuilder.Build();
        
        var example = schemas["Example"];
        var inner = schemas["Inner"];

        var result = new OpenApiSchemaComparer().Compare(example, inner);

        Assert.NotEmpty(result);
    }

}
