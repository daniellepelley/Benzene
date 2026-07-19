using System.Linq;
using System.Reflection;
using Benzene.Abstractions.Validation;
using Benzene.FluentValidation.Schema;
using Benzene.Test.Plugins.FluentValidation.Examples;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class SchemaTest
{
    [Theory]
    [InlineData(nameof(TestValidationObject.IsGuid), ValidationConstants.IsGuid)]
    [InlineData(nameof(TestValidationObject.IsDoubleGuid), ValidationConstants.IsDoubleGuid)]
    [InlineData(nameof(TestValidationObject.IsNumeric), ValidationConstants.IsNumeric)]
    [InlineData(nameof(TestValidationObject.IsBoolean), ValidationConstants.IsBoolean)]
    public void FluentValidationSchemaBuilder_MapsEachFormatRuleToItsOwnSchemaName(string property, string expectedSchemaName)
    {
        var fluentValidationSchemaBuilder = new FluentValidationSchemaBuilder(new TestValidator());

        var validationSchemas = fluentValidationSchemaBuilder.GetValidationSchemas(typeof(TestValidationObject));

        // Each format rule must reflect into a schema named after ITS OWN constant - IsDoubleGuid was
        // a copy-paste of the IsNumeric case, so a double-GUID field rendered as "IsNumeric" in the
        // generated OpenAPI/benzene spec.
        Assert.Equal(expectedSchemaName, validationSchemas[property].Single().Name);
    }

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
