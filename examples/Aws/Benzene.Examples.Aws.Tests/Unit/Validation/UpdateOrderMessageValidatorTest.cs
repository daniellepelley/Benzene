using System;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Validators;
using Benzene.Examples.Aws.Tests.Helpers.Builders;
using FluentValidation.TestHelper;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Unit.Validation;

public class UpdateOrderMessageValidatorTest
{
    [Fact]
    public void IsValid()
    {
        var validator = new UpdateOrderMessageValidator();
        var result = validator.TestValidate(new UpdateOrderMessage
        {
            Id = Guid.NewGuid().ToString(),
            Status = Defaults.Order.Status,
            Name = Defaults.Order.Name,

        });
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void StatusIsNotMandatory()
    {
        var validator = new UpdateOrderMessageValidator();
        var result = validator.TestValidate(new UpdateOrderMessage
        {
            Id = Guid.NewGuid().ToString(),
            Status = ""
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void NameIsNotMandatory()
    {
        var validator = new UpdateOrderMessageValidator();
        var result = validator.TestValidate(new UpdateOrderMessage
        {
            Id = Guid.NewGuid().ToString(),
            Name = ""
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void IsInvalidName()
    {
        var validator = new UpdateOrderMessageValidator();
        var result = validator.TestValidate(new UpdateOrderMessage
        {
            Id = Guid.NewGuid().ToString(),
            Name = "12345678901234567890123456789012345678901234567890"
        });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void IsInvalidIfNull()
    {
        var validator = new UpdateOrderMessageValidator();
        var result = validator.TestValidate(new UpdateOrderMessage
        {
            Id = Guid.NewGuid().ToString(),
            Status = null
        });
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }
}