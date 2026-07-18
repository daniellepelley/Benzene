using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Azure.EventGrid;
using Benzene.Results;
using Benzene.Test.Examples;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Azure.EventGrid;

public class EventGridContextConverterTest
{
    private const string Source = "my-service";

    [Fact]
    public async Task CreateRequestAsync_SetsCloudEventType()
    {
        var converter = new EventGridContextConverter<ExampleRequestPayload>(Source);
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.NotNull(context.CloudEvent);
        Assert.Null(context.EventGridEvent);
        Assert.Equal(Defaults.Topic, context.CloudEvent.Type);
        Assert.Equal(Source, context.CloudEvent.Source);
    }

    [Fact]
    public async Task CreateRequestAsync_ForwardsHeadersAsLowercasedExtensionAttributes()
    {
        // CloudEvents requires lower-case-only extension attribute names, so a mixed-case header key
        // (e.g. the default "correlationId") must be lowercased or the SDK throws at send time.
        var converter = new EventGridContextConverter<ExampleRequestPayload>(Source);
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "tenantId", "tenant-1" } });
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.Equal("tenant-1", context.CloudEvent.ExtensionAttributes["tenantid"]);
    }

    [Fact]
    public async Task MapResponseAsync_SetsAcceptedResult()
    {
        var converter = new EventGridContextConverter<ExampleRequestPayload>(Source);
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);
        var context = await converter.CreateRequestAsync(contextIn);

        await converter.MapResponseAsync(contextIn, context);

        Assert.Equal(BenzeneResultStatus.Accepted, contextIn.Response.Status);
    }
}

public class EventGridEventSchemaContextConverterTest
{
    [Fact]
    public async Task CreateRequestAsync_SetsSubjectAndEventType()
    {
        var converter = new EventGridEventSchemaContextConverter<ExampleRequestPayload>();
        var request = new BenzeneClientRequest<ExampleRequestPayload>(Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" }, new Dictionary<string, string>());
        var contextIn = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var context = await converter.CreateRequestAsync(contextIn);

        Assert.NotNull(context.EventGridEvent);
        Assert.Null(context.CloudEvent);
        Assert.Equal(Defaults.Topic, context.EventGridEvent.EventType);
        Assert.Equal(Defaults.Topic, context.EventGridEvent.Subject);
    }
}
