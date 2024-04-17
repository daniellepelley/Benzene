using System;
using Benzene.Clients.Aws.Common;
using Benzene.Clients.Aws.Lambda;
using Benzene.Results;
using Benzene.Test.Clients.Aws.Samples;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = Benzene.Clients.JsonSerializer;

namespace Benzene.Test.Clients.Aws.Lambda;

public class LambdaResultExtensionTest
{
    [Theory]
    [InlineData("200", ClientResultStatus.Ok)]
    [InlineData("201", ClientResultStatus.Created)]
    [InlineData("204", ClientResultStatus.Ok)]
    public void MapSuccessTest(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode,
            JsonConvert.SerializeObject(new ExamplePayload { Name = "some-name" }));

        var lambdaClientResult = lambdaResponse.AsClientResult<ExamplePayload>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Equal("some-name", lambdaClientResult.Payload.Name);
    }

    [Theory]
    [InlineData("200", ClientResultStatus.Ok)]
    [InlineData("201", ClientResultStatus.Created)]
    [InlineData("204", ClientResultStatus.Ok)]
    public void MapSuccessTest_NullPayload(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, null);

        var lambdaClientResult = lambdaResponse.AsClientResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Empty(lambdaClientResult.Errors);
    }

    [Theory]
    [InlineData("200", ClientResultStatus.Ok)]
    [InlineData("201", ClientResultStatus.Created)]
    [InlineData("204", ClientResultStatus.Ok)]
    public void MapSuccessTest_NullDefaultString(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(null));

        var lambdaClientResult = lambdaResponse.AsClientResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Empty(lambdaClientResult.Errors);
    }

    [Theory]
    [InlineData("200", ClientResultStatus.Ok)]
    [InlineData("201", ClientResultStatus.Created)]
    [InlineData("204", ClientResultStatus.Ok)]
    public void MapSuccessTest_HandleGuid(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject("b2d20bc3-9e29-4164-9983-1b568a1b70be"));

        var lambdaClientResult = lambdaResponse.AsClientResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Empty(lambdaClientResult.Errors);
        Assert.Equal(Guid.Parse("b2d20bc3-9e29-4164-9983-1b568a1b70be"), lambdaClientResult.Payload);
    }

    [Theory]
    [InlineData("400", ClientResultStatus.BadRequest)]
    [InlineData("401", ClientResultStatus.Unauthorized)]
    [InlineData("403", ClientResultStatus.Forbidden)]
    [InlineData("404", ClientResultStatus.NotFound)]
    [InlineData("409", ClientResultStatus.Conflict)]
    [InlineData("422", ClientResultStatus.ValidationError)]
    [InlineData("503", ClientResultStatus.ServiceUnavailable)]
    public void MapFailureTestGuid(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(new { Errors = new[] { "some-error" } }));
        var lambdaClientResult = lambdaResponse.AsClientResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Equal("some-error", lambdaClientResult.Errors[0]);
    }

    [Theory]
    [InlineData("400", ClientResultStatus.BadRequest)]
    [InlineData("401", ClientResultStatus.Unauthorized)]
    [InlineData("403", ClientResultStatus.Forbidden)]
    [InlineData("404", ClientResultStatus.NotFound)]
    [InlineData("409", ClientResultStatus.Conflict)]
    [InlineData("422", ClientResultStatus.ValidationError)]
    [InlineData("503", ClientResultStatus.ServiceUnavailable)]
    public void MapFailureTestObject(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(new { Errors = new[] { "some-error" } }));

        var lambdaClientResult = lambdaResponse.AsClientResult<object>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Equal("some-error", lambdaClientResult.Errors[0]);
    }

    [Theory]
    [InlineData("400", ClientResultStatus.BadRequest)]
    [InlineData("401", ClientResultStatus.Unauthorized)]
    [InlineData("403", ClientResultStatus.Forbidden)]
    [InlineData("404", ClientResultStatus.NotFound)]
    [InlineData("409", ClientResultStatus.Conflict)]
    [InlineData("422", ClientResultStatus.ValidationError)]
    [InlineData("503", ClientResultStatus.ServiceUnavailable)]
    public void MapFailureTest_NullPayload(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, null);

        var lambdaClientResult = lambdaResponse.AsClientResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Empty(lambdaClientResult.Errors);
    }

    [Theory]
    [InlineData("400", ClientResultStatus.BadRequest)]
    [InlineData("401", ClientResultStatus.Unauthorized)]
    [InlineData("403", ClientResultStatus.Forbidden)]
    [InlineData("404", ClientResultStatus.NotFound)]
    [InlineData("409", ClientResultStatus.Conflict)]
    [InlineData("422", ClientResultStatus.ValidationError)]
    [InlineData("503", ClientResultStatus.ServiceUnavailable)]
    public void MapFailureTest_NullStringPayload(string responseStatusCode, string expectedStatus)
    {
        var lambdaResponse = new BenzeneMessageClientResponse(responseStatusCode, JsonConvert.SerializeObject(null));

        var lambdaClientResult = lambdaResponse.AsClientResult<Guid>(new JsonSerializer());

        Assert.Equal(expectedStatus, lambdaClientResult.Status);
        Assert.Null(lambdaClientResult.Errors);
    }
}
