using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Validators;
using Benzene.Examples.Aws.Tests.Helpers.Builders;
using FluentValidation.TestHelper;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Unit.Validation;

public class CreateOrderMessageValidatorTest
{
    [Fact]
    public void IsValid()
    {
        var validator = new CreateOrderMessageValidator();
        var result = validator.TestValidate(new CreateOrderMessage
        {
            Name = Defaults.Order.Name,
            Status = Defaults.Order.Status,
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CRNIsNotMandatory()
    {
        var validator = new CreateOrderMessageValidator();
        var result = validator.TestValidate(new CreateOrderMessage
        {
            Status = "1",
            Name = "12345678901234567890123456789012345678901234567890"
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void IsNullValidation()
    {
        var validator = new CreateOrderMessageValidator();
        var result = validator.TestValidate(new CreateOrderMessage
        {
            Name = null,
            Status = null
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}