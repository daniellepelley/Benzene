using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Clients;
using Benzene.Results;
using Benzene.Test.Clients.Samples;
using Moq;
using Xunit;

namespace Benzene.Test.Clients;

public class RetryAwsLambdaClientTest
{
    private const string Topic = "some-topic";

    [Fact]
    public async Task Retries_Success_Test()
    {
        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        mockAwsLambdaClient.Setup(x =>
                x.SendMessageAsync<ExamplePayload, ExamplePayload>(It.IsAny<IBenzeneClientRequest<ExamplePayload>>()))
            .ReturnsAsync(BenzeneResult.Ok<ExamplePayload>());
        using var retryClient = new RetryBenzeneMessageClient(mockAwsLambdaClient.Object);
        await retryClient.SendMessageAsync<ExamplePayload, ExamplePayload>(Topic, new ExamplePayload(),  new Dictionary<string, string>());
        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(It.IsAny<IBenzeneClientRequest<ExamplePayload>>()), Times.Exactly(1));
    }

    [Fact]
    public async Task Retries_ServiceUnavailable_Test()
    {
        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        mockAwsLambdaClient.Setup(x =>
                x.SendMessageAsync<ExamplePayload, ExamplePayload>(It.IsAny<IBenzeneClientRequest<ExamplePayload>>()))
            .ReturnsAsync(BenzeneResult.ServiceUnavailable<ExamplePayload>());

        using var retryClient = new RetryBenzeneMessageClient(mockAwsLambdaClient.Object);

        await retryClient.SendMessageAsync<ExamplePayload, ExamplePayload>(Topic, new ExamplePayload(), new Dictionary<string, string>());

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(It.IsAny<IBenzeneClientRequest<ExamplePayload>>()), Times.Exactly(3));
    }
}
