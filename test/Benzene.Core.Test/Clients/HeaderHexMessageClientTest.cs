using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Test.Clients.Samples;
using Moq;
using Xunit;

namespace Benzene.Test.Clients;

public class HeaderBenzeneMessageClientTest
{
    [Fact]
    public async Task SendMessageAsync()
    {
        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        var client = new HeaderBenzeneMessageClient(mockAwsLambdaClient.Object, "some-key", "some-value");
        await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload(),
            new Dictionary<string, string>());

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(
            "some-topic",
            It.IsAny<ExamplePayload>(),
            It.Is<IDictionary<string, string>>(d => d["some-key"] == "some-value")));
    }

    [Fact]
    public async Task SendMessageAsync_NullDictionary()
    {
        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        var client = new HeaderBenzeneMessageClient(mockAwsLambdaClient.Object, "some-key", "some-value");
        await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload());

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(
            "some-topic",
            It.IsAny<ExamplePayload>(),
            It.Is<IDictionary<string, string>>(d => d["some-key"] == "some-value")));
    }

    [Fact]
    public async Task SendMessageAsync_PopulatedDictionary()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "some-key", "value" }
        };

        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        var client = new HeaderBenzeneMessageClient(mockAwsLambdaClient.Object, "some-key", "some-value");
        await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload(),  dictionary);

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(
            "some-topic",
            It.IsAny<ExamplePayload>(),
            It.Is<IDictionary<string, string>>(d => d["some-key"] == "some-value")));
    }
}
