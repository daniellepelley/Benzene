using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Azure.EventHub;
using Benzene.Results;
using Benzene.Test.Examples;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Azure.EventHub;

public class EventHubContextConverterTest
{
    [Fact]
    public async Task CreateRequestAsync_SetsTopicProperty()
    {
        var converter = new EventHubContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Equal(Defaults.Topic, context.EventData.Properties["topic"]);
    }

    [Fact]
    public async Task CreateRequestAsync_ForwardsHeadersAsProperties()
    {
        var converter = new EventHubContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "tenantId", "tenant-1" } });
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Equal("tenant-1", context.EventData.Properties["tenantId"]);
    }

    [Fact]
    public async Task CreateRequestAsync_SerializesMessageAsBody()
    {
        var converter = new EventHubContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Contains("foo", Encoding.UTF8.GetString(context.EventData.EventBody.ToArray()));
    }

    [Fact]
    public async Task MapResponseAsync_SetsAcceptedResult()
    {
        var converter = new EventHubContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);
        var context = await converter.CreateRequestAsync(contextIn);

        await converter.MapResponseAsync(contextIn, context);

        Assert.Equal(BenzeneResultStatus.Accepted, contextIn.Response.Status);
    }
}
