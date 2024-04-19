using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Results;
using Benzene.Test.Clients.Samples;
using Moq;
using Xunit;

namespace Benzene.Test.Clients;

public class HealthCheckTest
{
    private const string Topic = "some-topic";

    [Fact]
    public async Task Retries_Success_Test()
    {
        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        mockAwsLambdaClient.Setup(x =>
                x.SendMessageAsync<ExamplePayload, ExamplePayload>(It.IsAny<string>(), It.IsAny<ExamplePayload>(), It.IsAny<IDictionary<string, string>>()))
            .ReturnsAsync(ClientResult.Ok<ExamplePayload>());

        var r = new RetryBenzeneMessageClient(mockAwsLambdaClient.Object);

        await r.SendMessageAsync<ExamplePayload, ExamplePayload>(Topic, new ExamplePayload());

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(Topic, It.IsAny<ExamplePayload>(), It.IsAny<IDictionary<string, string>>()), Times.Exactly(1));
    }

    [Fact]
    public async Task Retries_ServiceUnavailable_Test()
    {
        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        mockAwsLambdaClient.Setup(x =>
                x.SendMessageAsync<ExamplePayload, ExamplePayload>(It.IsAny<string>(), It.IsAny<ExamplePayload>(),  It.IsAny<IDictionary<string, string>>()))
            .ReturnsAsync(ClientResult.ServiceUnavailable<ExamplePayload>());

        var r = new RetryBenzeneMessageClient(mockAwsLambdaClient.Object);

        await r.SendMessageAsync<ExamplePayload, ExamplePayload>(Topic, new ExamplePayload());

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(Topic, It.IsAny<ExamplePayload>(), It.IsAny<IDictionary<string, string>>()), Times.Exactly(3));
    }
}
