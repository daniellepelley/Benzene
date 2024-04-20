using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Unit.Validation;

public class DeleteOrderMessageValidatorTest
{
    [Fact]
    public void IsValid()
    {
        var validator = new DeleteOrderMessageValidator();
        var result = validator.TestValidate(new DeleteOrderMessage
        {
            Id = "f1b5e465-48c4-43ae-b4a6-2a6069836cf8"
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void IsInvalid()
    {
        var validator = new DeleteOrderMessageValidator();
        var result = validator.TestValidate(new DeleteOrderMessage
        {
            Id = ""
        });
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}