using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Azure.QueueStorage;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;
using Benzene.Test.Examples;
using Newtonsoft.Json;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Azure.QueueStorage;

public class QueueStorageContextConverterTest
{
    [Fact]
    public async Task CreateRequestAsync_SerializesBenzeneMessageEnvelope()
    {
        var converter = new QueueStorageContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "tenantId", "tenant-1" } });
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);
        var envelope = JsonConvert.DeserializeObject<BenzeneMessageRequest>(context.MessageText);

        Assert.Equal(Defaults.Topic, envelope.Topic);
        Assert.Equal("tenant-1", envelope.Headers["tenantId"]);
        Assert.Contains("foo", envelope.Body);
    }

    [Fact]
    public async Task MapResponseAsync_SetsAcceptedResult()
    {
        var converter = new QueueStorageContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);
        var context = await converter.CreateRequestAsync(contextIn);

        await converter.MapResponseAsync(contextIn, context);

        Assert.Equal(BenzeneResultStatus.Accepted, contextIn.Response.Status);
    }
}
