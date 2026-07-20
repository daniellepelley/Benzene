using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Azure.ServiceBus;
using Benzene.Results;
using Benzene.Test.Examples;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Azure.ServiceBus;

public class ServiceBusContextConverterTest
{
    [Fact]
    public async Task CreateRequestAsync_SetsTopicApplicationProperty()
    {
        var converter = new ServiceBusContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Equal(Defaults.Topic, context.Message.ApplicationProperties["topic"]);
    }

    [Fact]
    public async Task CreateRequestAsync_ForwardsHeadersAsApplicationProperties()
    {
        var converter = new ServiceBusContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "tenantId", "tenant-1" } });
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Equal("tenant-1", context.Message.ApplicationProperties["tenantId"]);
    }

    [Fact]
    public async Task CreateRequestAsync_SerializesMessageAsBody()
    {
        var converter = new ServiceBusContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Contains("foo", Encoding.UTF8.GetString(context.Message.Body));
    }

    [Fact]
    public async Task CreateRequestAsync_MapsConfiguredHeadersOntoBrokerProperties()
    {
        var converter = new ServiceBusContextConverter<ExampleRequestPayload>(
            senderProperties: new ServiceBusSenderProperties
            {
                MessageIdHeader = "x-message-id",
                SessionIdHeader = "x-session-id",
                ScheduledEnqueueTimeHeader = "x-scheduled",
                TimeToLiveHeader = "x-ttl"
            });
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string>
            {
                { "x-message-id", "msg-1" },
                { "x-session-id", "session-1" },
                { "x-scheduled", "2026-07-20T17:00:00Z" },
                { "x-ttl", "30" }
            });
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Equal("msg-1", context.Message.MessageId);
        Assert.Equal("session-1", context.Message.SessionId);
        Assert.Equal(new System.DateTimeOffset(2026, 7, 20, 17, 0, 0, System.TimeSpan.Zero), context.Message.ScheduledEnqueueTime);
        Assert.Equal(System.TimeSpan.FromSeconds(30), context.Message.TimeToLive);
    }

    [Fact]
    public async Task CreateRequestAsync_TimeToLive_AcceptsIso8601Duration()
    {
        var converter = new ServiceBusContextConverter<ExampleRequestPayload>(
            senderProperties: new ServiceBusSenderProperties { TimeToLiveHeader = "x-ttl" });
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "x-ttl", "PT2M" } });
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Equal(System.TimeSpan.FromMinutes(2), context.Message.TimeToLive);
    }

    [Fact]
    public async Task CreateRequestAsync_WithoutSenderProperties_LeavesBrokerPropertiesDefault()
    {
        var converter = new ServiceBusContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "x-session-id", "session-1" } });
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        // No sender-properties mapping configured: the header is still a plain application property,
        // but SessionId is not set from it.
        Assert.Null(context.Message.SessionId);
        Assert.Equal("session-1", context.Message.ApplicationProperties["x-session-id"]);
    }

    [Fact]
    public async Task MapResponseAsync_SetsAcceptedResult()
    {
        var converter = new ServiceBusContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);
        var context = await converter.CreateRequestAsync(contextIn);

        await converter.MapResponseAsync(contextIn, context);

        Assert.Equal(BenzeneResultStatus.Accepted, contextIn.Response.Status);
    }
}
