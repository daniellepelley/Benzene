using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Test.Clients.Samples;
using Moq;
using Xunit;

namespace Benzene.Test.Clients;

public class ClientHeadersAwsLambdaClientTest
{
    [Fact]
    public async Task SendMessageAsync()
    {
        var clientHeaders = new ClientHeaders();
        clientHeaders.Set("some-key", "some-value");

        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        using var client = new HeadersBenzeneMessageClient(mockAwsLambdaClient.Object, clientHeaders);
        await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload(), new Dictionary<string, string>());

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(
            "some-topic",
            It.IsAny<ExamplePayload>(),
            It.Is<IDictionary<string, string>>(d => d["some-key"] == "some-value")));
    }

    [Fact]
    public async Task SendMessageAsync_NullDictionary()
    {
        var clientHeaders = new ClientHeaders();
        clientHeaders.Set("some-key", "some-value");
            
        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        using var client = new HeadersBenzeneMessageClient(mockAwsLambdaClient.Object, clientHeaders);
        await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload());

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(
            "some-topic",
            It.IsAny<ExamplePayload>(),
            It.Is<IDictionary<string, string>>(d => d["some-key"] == "some-value")));
    }

    [Fact]
    public async Task SendMessageAsync_PopulatedDictionary()
    {
        var clientHeaders = new ClientHeaders();
        clientHeaders.Set("some-key", "some-value");
            
        var dictionary = new Dictionary<string, string>
        {
            { "some-key", "value" },
            { "some-other-key", "other-value"}
        };

        var mockAwsLambdaClient = new Mock<IBenzeneMessageClient>();

        using var client = new HeadersBenzeneMessageClient(mockAwsLambdaClient.Object, clientHeaders);
        await client.SendMessageAsync<ExamplePayload, ExamplePayload>("some-topic", new ExamplePayload(),  dictionary);

        mockAwsLambdaClient.Verify(x => x.SendMessageAsync<ExamplePayload, ExamplePayload>(
            "some-topic",
            It.IsAny<ExamplePayload>(),
            It.Is<IDictionary<string, string>>(d => 
                d["some-key"] == "some-value" && d["some-other-key"] == "other-value")));
    }
}
