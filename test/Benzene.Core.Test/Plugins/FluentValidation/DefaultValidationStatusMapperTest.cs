using Benzene.Abstractions.Validation;
using Benzene.FluentValidation;
using Benzene.Results;
using FluentValidation.Results;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class DefaultValidationStatusMapperTest
{
    [ValidationStatus(BenzeneResultStatus.Forbidden)]
    private class HandlerWithStatusAttribute
    {
    }

    private class HandlerWithoutStatusAttribute
    {
    }

    [Fact]
    public void GetStatus_ValidationResultHasACustomStateFailure_ReturnsItsStatus()
    {
        var mapper = new DefaultValidationStatusMapper();
        var result = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "is required") { CustomState = new BenzeneValidationState { Status = BenzeneResultStatus.BadRequest } }
        });

        var status = mapper.GetStatus(typeof(HandlerWithStatusAttribute), typeof(object), result);

        Assert.Equal(BenzeneResultStatus.BadRequest, status);
    }

    [Fact]
    public void GetStatus_NoCustomStateFailure_HandlerHasValidationStatusAttribute_ReturnsAttributeStatus()
    {
        var mapper = new DefaultValidationStatusMapper();
        var result = new ValidationResult(new[] { new ValidationFailure("Name", "is required") });

        var status = mapper.GetStatus(typeof(HandlerWithStatusAttribute), typeof(object), result);

        Assert.Equal(BenzeneResultStatus.Forbidden, status);
    }

    [Fact]
    public void GetStatus_NoCustomStateFailure_HandlerHasNoAttribute_ReturnsDefaultValidationError()
    {
        var mapper = new DefaultValidationStatusMapper();
        var result = new ValidationResult(new[] { new ValidationFailure("Name", "is required") });

        var status = mapper.GetStatus(typeof(HandlerWithoutStatusAttribute), typeof(object), result);

        Assert.Equal(BenzeneResultStatus.ValidationError, status);
    }

    [Fact]
    public void GetStatus_HandlerTypeIsNull_ReturnsDefaultValidationError()
    {
        var mapper = new DefaultValidationStatusMapper();
        var result = new ValidationResult(new[] { new ValidationFailure("Name", "is required") });

        var status = mapper.GetStatus(null, typeof(object), result);

        Assert.Equal(BenzeneResultStatus.ValidationError, status);
    }

    [Fact]
    public void GetStatus_ResultIsNotAValidationResult_FallsThroughToHandlerAttributeOrDefault()
    {
        var mapper = new DefaultValidationStatusMapper();

        var status = mapper.GetStatus(typeof(HandlerWithStatusAttribute), typeof(object), "not a validation result");

        Assert.Equal(BenzeneResultStatus.Forbidden, status);
    }
}
