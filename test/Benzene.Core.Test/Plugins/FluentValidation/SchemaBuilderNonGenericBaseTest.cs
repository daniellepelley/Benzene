using Benzene.FluentValidation.Schema;
using FluentValidation;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class SchemaBuilderNonGenericBaseTest
{
    private class Thing
    {
        public string Name { get; set; }
    }

    // A non-generic intermediate base between the concrete validator and AbstractValidator<T>. The
    // schema builder used to reflect the concrete validator's *direct* BaseType generic argument,
    // which threw IndexOutOfRangeException here (the direct base is not a closed generic).
    private abstract class IntermediateValidator : AbstractValidator<Thing>
    {
    }

    private class ThingValidator : IntermediateValidator
    {
        public ThingValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public void GetValidationSchemas_ValidatorWithNonGenericIntermediateBase_DoesNotThrow()
    {
        var builder = new FluentValidationSchemaBuilder(new ThingValidator());

        var schemas = builder.GetValidationSchemas(typeof(Thing));

        Assert.True(schemas.ContainsKey("Name"));
    }
}
