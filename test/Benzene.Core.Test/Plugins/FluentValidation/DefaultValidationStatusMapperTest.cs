using Benzene.Core.MessageHandlers;
using Benzene.FluentValidation;
using Benzene.Results;
using FluentValidation.Results;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class DefaultValidationStatusMapperTest
{
    private class CustomDefaultStatuses : IDefaultStatuses
    {
        public string ValidationError => "CustomValidationError";
        public string NotFound => BenzeneResultStatus.NotFound;
        public string BadRequest => BenzeneResultStatus.BadRequest;
        public string UnhandledException => BenzeneResultStatus.ServiceUnavailable;
    }

    [Fact]
    public void GetStatus_ValidationResultHasACustomStateFailure_ReturnsItsStatus()
    {
        var mapper = new DefaultValidationStatusMapper(new DefaultStatuses());
        var result = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "is required") { CustomState = new BenzeneValidationState { Status = BenzeneResultStatus.BadRequest } }
        });

        var status = mapper.GetStatus(null, typeof(object), result);

        Assert.Equal(BenzeneResultStatus.BadRequest, status);
    }

    [Fact]
    public void GetStatus_NoCustomStateFailure_ReturnsDefaultStatusesValidationError()
    {
        var mapper = new DefaultValidationStatusMapper(new DefaultStatuses());
        var result = new ValidationResult(new[] { new ValidationFailure("Name", "is required") });

        var status = mapper.GetStatus(null, typeof(object), result);

        Assert.Equal(BenzeneResultStatus.ValidationError, status);
    }

    [Fact]
    public void GetStatus_ResultIsNotAValidationResult_ReturnsDefaultStatusesValidationError()
    {
        var mapper = new DefaultValidationStatusMapper(new DefaultStatuses());

        var status = mapper.GetStatus(null, typeof(object), "not a validation result");

        Assert.Equal(BenzeneResultStatus.ValidationError, status);
    }

    [Fact]
    public void GetStatus_NoCustomStateFailure_UsesOverriddenIDefaultStatuses()
    {
        // The top-level override point: registering a different IDefaultStatuses changes what every
        // FluentValidation failure returns, with no per-handler variation possible.
        var mapper = new DefaultValidationStatusMapper(new CustomDefaultStatuses());
        var result = new ValidationResult(new[] { new ValidationFailure("Name", "is required") });

        var status = mapper.GetStatus(null, typeof(object), result);

        Assert.Equal("CustomValidationError", status);
    }
}
