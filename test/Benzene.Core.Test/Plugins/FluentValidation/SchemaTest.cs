using System.Linq;
using System.Reflection;
using Benzene.FluentValidation.Schema;
using Benzene.Test.Plugins.FluentValidation.Examples;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class SchemaTest
{
    [Fact]
    public void FluentValidationSchemaBuilder()
    {
        var fluentValidationSchemaBuilder = new FluentValidationSchemaBuilder(new TestValidator());

        var validationSchemas = fluentValidationSchemaBuilder.GetValidationSchemas(typeof(TestValidationObject));

        Assert.Equal(11, validationSchemas.Count);
        Assert.Equal("IsOneOf", validationSchemas.First().Key);
        Assert.Equal("IsOneOf", validationSchemas.First().Value.First().Name);
        Assert.Equal("Is one of 'one', 'two'", validationSchemas.First().Value.First().Description);
    }

    [Fact]
    public void FluentValidationSchemaBuilderFromAssembly()
    {
        var fluentValidationSchemaBuilder = new FluentValidationSchemaBuilder(Assembly.GetExecutingAssembly());

        var validationSchemas = fluentValidationSchemaBuilder.GetValidationSchemas(typeof(TestValidationObject));

        Assert.Equal(11, validationSchemas.Count);
        Assert.Equal("IsOneOf", validationSchemas.First().Key);
        Assert.Equal("IsOneOf", validationSchemas.First().Value.First().Name);
        Assert.Equal("Is one of 'one', 'two'", validationSchemas.First().Value.First().Description);
    }

}
