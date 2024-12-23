using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.Abstractions.Logging;
using Benzene.Clients.Aws.StepFunctions;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Clients.Aws.StepFunctions;

public class StepFunctionsClientTest
{
    [Fact]
    public async Task Start()
    {
        var mockAmazonStepFunctions = new Mock<IAmazonStepFunctions>();
        mockAmazonStepFunctions.Setup(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartExecutionResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        var client = new StepFunctionsClientFactory(Defaults.StateMachineArn, mockAmazonStepFunctions.Object, Mock.Of<IBenzeneLogger>()).Create();
        var result = await client.StartExecutionAsync<ExampleRequestPayload, ExampleResponsePayload>(new ExampleRequestPayload { Id = 42, Name = "hi" });

        mockAmazonStepFunctions.Verify(x => x.StartExecutionAsync(
                       It.Is<StartExecutionRequest>(message =>
                           message.StateMachineArn == Defaults.StateMachineArn &&
                           JsonConvert.DeserializeObject<ExampleRequestPayload>(message.Input).Name == "hi"
                           ), It.IsAny<CancellationToken>()));

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task Start_Exception()
    {
        var mockAmazonStepFunctions = new Mock<IAmazonStepFunctions>();
        mockAmazonStepFunctions.Setup(x => x.StartExecutionAsync(It.IsAny<StartExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception());

        var client = new StepFunctionsClientFactory(Defaults.StateMachineArn, mockAmazonStepFunctions.Object, Mock.Of<IBenzeneLogger>()).Create();
        var result = await client.StartExecutionAsync<ExampleRequestPayload, ExampleResponsePayload>(new ExampleRequestPayload { Id = 42, Name = "hi" });

        mockAmazonStepFunctions.Verify(x => x.StartExecutionAsync(
                       It.Is<StartExecutionRequest>(message =>
                           message.StateMachineArn == Defaults.StateMachineArn &&
                           JsonConvert.DeserializeObject<ExampleRequestPayload>(message.Input).Name == "hi"
                           ), It.IsAny<CancellationToken>()));

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }
}
