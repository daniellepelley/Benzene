using System.Net;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Core.Results;

public class BenzeneResultStatusTest
{
    [Theory]
    [InlineData(BenzeneResultStatus.Ok)]
    [InlineData(BenzeneResultStatus.Created)]
    [InlineData(BenzeneResultStatus.Accepted)]
    [InlineData(BenzeneResultStatus.Updated)]
    [InlineData(BenzeneResultStatus.Deleted)]
    [InlineData(BenzeneResultStatus.Ignored)]
    public void IsSuccess_TrueForSuccessStatuses(string status)
    {
        Assert.True(BenzeneResultStatus.IsSuccess(status));
        Assert.False(BenzeneResultStatus.IsFailure(status));
        Assert.True(BenzeneResultStatus.IsKnown(status));
    }

    [Theory]
    [InlineData(BenzeneResultStatus.BadRequest)]
    [InlineData(BenzeneResultStatus.ValidationError)]
    [InlineData(BenzeneResultStatus.Unauthorized)]
    [InlineData(BenzeneResultStatus.Forbidden)]
    [InlineData(BenzeneResultStatus.NotFound)]
    [InlineData(BenzeneResultStatus.Conflict)]
    [InlineData(BenzeneResultStatus.TooManyRequests)]
    [InlineData(BenzeneResultStatus.Timeout)]
    [InlineData(BenzeneResultStatus.NotImplemented)]
    [InlineData(BenzeneResultStatus.ServiceUnavailable)]
    [InlineData(BenzeneResultStatus.UnexpectedError)]
    public void IsFailure_TrueForFailureStatuses(string status)
    {
        Assert.True(BenzeneResultStatus.IsFailure(status));
        Assert.False(BenzeneResultStatus.IsSuccess(status));
        Assert.True(BenzeneResultStatus.IsKnown(status));
    }

    [Theory]
    [InlineData("SomeCustomStatus")]
    [InlineData("200")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownStatuses_AreNeitherSuccessNorFailure(string? status)
    {
        Assert.False(BenzeneResultStatus.IsSuccess(status));
        Assert.False(BenzeneResultStatus.IsFailure(status));
        Assert.False(BenzeneResultStatus.IsKnown(status));
        Assert.False(BenzeneResultStatus.IsTransient(status));
    }

    [Theory]
    [InlineData(BenzeneResultStatus.ServiceUnavailable, true)]
    [InlineData(BenzeneResultStatus.TooManyRequests, true)]
    [InlineData(BenzeneResultStatus.Timeout, true)]
    [InlineData(BenzeneResultStatus.UnexpectedError, false)]
    [InlineData(BenzeneResultStatus.NotFound, false)]
    [InlineData(BenzeneResultStatus.Ok, false)]
    public void IsTransient_TrueOnlyForTransientStatuses(string status, bool expected)
    {
        Assert.Equal(expected, BenzeneResultStatus.IsTransient(status));
    }

    [Fact]
    public void NewFactories_ProduceFailureResultsWithErrors()
    {
        var tooManyRequests = BenzeneResult.TooManyRequests<string>("throttled");
        Assert.Equal(BenzeneResultStatus.TooManyRequests, tooManyRequests.Status);
        Assert.False(tooManyRequests.IsSuccessful);
        Assert.True(tooManyRequests.IsTooManyRequests());
        Assert.True(tooManyRequests.IsTransient());
        Assert.Contains("throttled", tooManyRequests.Errors);

        var timeout = BenzeneResult.Timeout("deadline elapsed");
        Assert.Equal(BenzeneResultStatus.Timeout, timeout.Status);
        Assert.False(timeout.IsSuccessful);
        Assert.True(timeout.IsTimeout());
        Assert.True(timeout.IsTransient());
        Assert.Contains("deadline elapsed", timeout.Errors);
    }

    [Fact]
    public void Set_DerivesIsSuccessfulFromStatusClass()
    {
        // Known failure statuses are unsuccessful even via the payload/void overloads. (Non-string
        // payloads — a string payload binds to the params-errors overload.)
        Assert.False(BenzeneResult.Set(BenzeneResultStatus.NotFound).IsSuccessful);
        Assert.False(BenzeneResult.Set(BenzeneResultStatus.Timeout, 42).IsSuccessful);

        // Success statuses stay successful.
        Assert.True(BenzeneResult.Set(BenzeneResultStatus.Ok).IsSuccessful);
        Assert.True(BenzeneResult.Set(BenzeneResultStatus.Updated, 42).IsSuccessful);

        // Application-defined statuses keep the historical successful default.
        Assert.True(BenzeneResult.Set("SomeCustomStatus").IsSuccessful);
        Assert.True(BenzeneResult.Set("200", 42).IsSuccessful);

        // The explicit overloads remain the escape hatch, payload or not.
        Assert.False(BenzeneResult.Set<string>("SomeCustomStatus", false).IsSuccessful);
        var explicitWithPayload = BenzeneResult.Set(BenzeneResultStatus.ServiceUnavailable, 42, true);
        Assert.True(explicitWithPayload.IsSuccessful);
        Assert.Equal(42, explicitWithPayload.Payload);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, BenzeneResultStatus.TooManyRequests)]
    [InlineData(HttpStatusCode.RequestTimeout, BenzeneResultStatus.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, BenzeneResultStatus.Timeout)]
    [InlineData(HttpStatusCode.BadGateway, BenzeneResultStatus.ServiceUnavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, BenzeneResultStatus.ServiceUnavailable)]
    [InlineData(HttpStatusCode.UnprocessableEntity, BenzeneResultStatus.ValidationError)]
    [InlineData(HttpStatusCode.NotImplemented, BenzeneResultStatus.NotImplemented)]
    public void Convert_MapsHttpStatusCodesToResultStatuses(HttpStatusCode httpStatusCode, string expectedStatus)
    {
        Assert.Equal(expectedStatus, httpStatusCode.Convert().Status);
        Assert.Equal(expectedStatus, httpStatusCode.Convert<string>().Status);
        Assert.False(httpStatusCode.Convert().IsSuccessful);
    }
}
