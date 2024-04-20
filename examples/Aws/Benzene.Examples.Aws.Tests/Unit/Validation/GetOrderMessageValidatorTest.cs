using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Unit.Validation;

public class GetOrderMessageValidatorTest
{
    [Fact]
    public void IsValid()
    {
        var validator = new GetOrderMessageValidator();
        var result = validator.TestValidate(new GetOrderMessage
        {
            Id = "abda4017-7f29-4081-ad5f-58d07d9dff13"
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void IsInvalid()
    {
        var validator = new GetOrderMessageValidator();
        var result = validator.TestValidate(new GetOrderMessage
        {
            Id = ""
        });
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}