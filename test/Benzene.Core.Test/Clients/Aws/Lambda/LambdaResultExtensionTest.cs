using System;
using Benzene.Clients;
using Benzene.Clients.Common;
using Benzene.Results;
using Benzene.Test.Clients.Aws.Samples;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = Benzene.Clients.JsonSerializer;

namespace Benzene.Test.Clients.Aws.Lambda;

public class LambdaResultExtensionTest
{
    [Theory]
    [InlineData("200", BenzeneResultStatus.Ok)]
    [InlineData("201", BenzeneResultStatus.Created)]
    [InlineData("204", BenzeneResultStatus.Ok)]
    public void MapSuccessTest(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode,
            JsonConvert.SerializeObject(new ExamplePayload { Name = "some-name" }));

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<ExamplePayload>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Equal("some-name", lambdaBenzeneResult.Payload.Name);
    }

    [Theory]
    [InlineData("200", BenzeneResultStatus.Ok)]
    [InlineData("201", BenzeneResultStatus.Created)]
    [InlineData("204", BenzeneResultStatus.Ok)]
    public void MapSuccessTest_NullPayload(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, null);

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Empty(lambdaBenzeneResult.Errors);
    }

    [Theory]
    [InlineData("200", BenzeneResultStatus.Ok)]
    [InlineData("201", BenzeneResultStatus.Created)]
    [InlineData("204", BenzeneResultStatus.Ok)]
    public void MapSuccessTest_NullDefaultString(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(null));

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Empty(lambdaBenzeneResult.Errors);
    }

    [Theory]
    [InlineData("200", BenzeneResultStatus.Ok)]
    [InlineData("201", BenzeneResultStatus.Created)]
    [InlineData("204", BenzeneResultStatus.Ok)]
    public void MapSuccessTest_HandleGuid(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject("b2d20bc3-9e29-4164-9983-1b568a1b70be"));

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Empty(lambdaBenzeneResult.Errors);
        Assert.Equal(Guid.Parse("b2d20bc3-9e29-4164-9983-1b568a1b70be"), lambdaBenzeneResult.Payload);
    }

    [Theory]
    [InlineData("400", BenzeneResultStatus.BadRequest)]
    [InlineData("401", BenzeneResultStatus.Unauthorized)]
    [InlineData("403", BenzeneResultStatus.Forbidden)]
    [InlineData("404", BenzeneResultStatus.NotFound)]
    [InlineData("409", BenzeneResultStatus.Conflict)]
    [InlineData("422", BenzeneResultStatus.ValidationError)]
    [InlineData("503", BenzeneResultStatus.ServiceUnavailable)]
    public void MapFailureTestGuid(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(new ProblemDetails { Detail = "some-error" }));
        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Equal("some-error", lambdaBenzeneResult.Errors[0]);
    }

    [Theory]
    [InlineData("400", BenzeneResultStatus.BadRequest)]
    [InlineData("401", BenzeneResultStatus.Unauthorized)]
    [InlineData("403", BenzeneResultStatus.Forbidden)]
    [InlineData("404", BenzeneResultStatus.NotFound)]
    [InlineData("409", BenzeneResultStatus.Conflict)]
    [InlineData("422", BenzeneResultStatus.ValidationError)]
    [InlineData("503", BenzeneResultStatus.ServiceUnavailable)]
    public void MapFailureTestObject(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(new ProblemDetails { Detail = "some-error" }));

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<object>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Equal("some-error", lambdaBenzeneResult.Errors[0]);
    }

    [Theory]
    [InlineData("400", BenzeneResultStatus.BadRequest)]
    [InlineData("401", BenzeneResultStatus.Unauthorized)]
    [InlineData("403", BenzeneResultStatus.Forbidden)]
    [InlineData("404", BenzeneResultStatus.NotFound)]
    [InlineData("409", BenzeneResultStatus.Conflict)]
    [InlineData("422", BenzeneResultStatus.ValidationError)]
    [InlineData("503", BenzeneResultStatus.ServiceUnavailable)]
    public void MapFailureTest_NullPayload(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, null);

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Empty(lambdaBenzeneResult.Errors);
    }

    [Theory]
    [InlineData("400", BenzeneResultStatus.BadRequest)]
    [InlineData("401", BenzeneResultStatus.Unauthorized)]
    [InlineData("403", BenzeneResultStatus.Forbidden)]
    [InlineData("404", BenzeneResultStatus.NotFound)]
    [InlineData("409", BenzeneResultStatus.Conflict)]
    [InlineData("422", BenzeneResultStatus.ValidationError)]
    [InlineData("503", BenzeneResultStatus.ServiceUnavailable)]
    public void MapFailureTest_NullStringPayload(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(null));

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaBenzeneResult.Status);
        Assert.Empty(lambdaBenzeneResult.Errors);
    }

    // The BenzeneMessage envelope carries raw Benzene statuses (docs/specification/wire-contracts.md),
    // which the client preserves verbatim - including the ones that would collapse under the numeric
    // HTTP mapping (Updated/Deleted -> 204 -> Ok).
    [Theory]
    [InlineData(BenzeneResultStatus.Ok)]
    [InlineData(BenzeneResultStatus.Created)]
    [InlineData(BenzeneResultStatus.Accepted)]
    [InlineData(BenzeneResultStatus.Updated)]
    [InlineData(BenzeneResultStatus.Deleted)]
    [InlineData(BenzeneResultStatus.Ignored)]
    public void MapSuccessTest_RawBenzeneStatusIsPreservedVerbatim(string benzeneStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(benzeneStatus,
            JsonConvert.SerializeObject(new ExamplePayload { Name = "some-name" }));

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<ExamplePayload>(new JsonSerializer());

        Assert.Equal(benzeneStatus, lambdaBenzeneResult.Status);
        Assert.True(lambdaBenzeneResult.IsSuccessful);
        Assert.Equal("some-name", lambdaBenzeneResult.Payload.Name);
    }

    [Theory]
    [InlineData(BenzeneResultStatus.BadRequest)]
    [InlineData(BenzeneResultStatus.ValidationError)]
    [InlineData(BenzeneResultStatus.Unauthorized)]
    [InlineData(BenzeneResultStatus.Forbidden)]
    [InlineData(BenzeneResultStatus.NotFound)]
    [InlineData(BenzeneResultStatus.Conflict)]
    [InlineData(BenzeneResultStatus.NotImplemented)]
    [InlineData(BenzeneResultStatus.ServiceUnavailable)]
    [InlineData(BenzeneResultStatus.UnexpectedError)]
    public void MapFailureTest_RawBenzeneStatusIsPreservedVerbatim(string benzeneStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(benzeneStatus,
            JsonConvert.SerializeObject(new ProblemDetails { Detail = "some-error" }));

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<ExamplePayload>(new JsonSerializer());

        Assert.Equal(benzeneStatus, lambdaBenzeneResult.Status);
        Assert.False(lambdaBenzeneResult.IsSuccessful);
        Assert.Equal("some-error", lambdaBenzeneResult.Errors[0]);
    }

    [Fact]
    public void Map_UnrecognizedStatusCode_ReturnsUnexpectedError()
    {
        var lambdaResponse = new BenzeneMessageClientResponse("999", null);

        var lambdaBenzeneResult = lambdaResponse.AsBenzeneResult<ExamplePayload>(new JsonSerializer());

        Assert.Equal(BenzeneResultStatus.UnexpectedError, lambdaBenzeneResult.Status);
        Assert.False(lambdaBenzeneResult.IsSuccessful);
    }
}
