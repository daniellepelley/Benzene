using System.Threading.Tasks;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventGrid;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class EventGridPipelineTest
{
    private static IAzureFunctionApp CreateApp(Mock<IExampleService> mockExampleService)
    {
        return new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseEventGrid(eventGrid => eventGrid
                    .UseMessageHandlers()))
            .Build();
    }

    [Fact]
    public async Task EventGridSchemaEvent_RoutesByEventType_AndDeliversDataAsPayload()
    {
        var mockExampleService = new Mock<IExampleService>();
        var app = CreateApp(mockExampleService);

        var eventJson = $$"""
        {
            "id": "event-1",
            "topic": "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/acct",
            "subject": "/blobServices/default/containers/orders",
            "eventType": "{{Defaults.Topic}}",
            "eventTime": "2026-07-17T10:00:00Z",
            "dataVersion": "1.0",
            "data": {{Defaults.Message}}
        }
        """;

        await app.HandleEventGridEvent(eventJson);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task CloudEventsSchemaEvent_RoutesByType_AndDeliversDataAsPayload()
    {
        var mockExampleService = new Mock<IExampleService>();
        var app = CreateApp(mockExampleService);

        var eventJson = $$"""
        {
            "specversion": "1.0",
            "id": "event-2",
            "source": "/mycontext",
            "subject": "orders/42",
            "type": "{{Defaults.Topic}}",
            "time": "2026-07-17T10:00:00Z",
            "data": {{Defaults.Message}}
        }
        """;

        await app.HandleEventGridEvent(eventJson);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public void Parse_EventGridSchema_MapsEnvelopeFields()
    {
        var parsed = EventGridTriggerEvent.Parse("""
        {
            "id": "event-1",
            "topic": "/subscriptions/sub",
            "subject": "some-subject",
            "eventType": "Custom.Event",
            "eventTime": "2026-07-17T10:00:00Z",
            "dataVersion": "2.1",
            "data": { "name": "value" }
        }
        """);

        Assert.Equal("event-1", parsed.Id);
        Assert.Equal("Custom.Event", parsed.EventType);
        Assert.Equal("some-subject", parsed.Subject);
        Assert.Equal("/subscriptions/sub", parsed.Source);
        Assert.Equal("2.1", parsed.DataVersion);
        Assert.NotNull(parsed.EventTime);
        Assert.NotNull(parsed.Data);
    }

    [Fact]
    public void Parse_CloudEventsSchema_MapsTypeAndSource()
    {
        var parsed = EventGridTriggerEvent.Parse("""
        {
            "specversion": "1.0",
            "id": "event-2",
            "source": "/mycontext",
            "type": "Custom.Event",
            "time": "2026-07-17T10:00:00Z"
        }
        """);

        Assert.Equal("Custom.Event", parsed.EventType);
        Assert.Equal("/mycontext", parsed.Source);
        Assert.Null(parsed.Data);
    }

    [Fact]
    public async Task EnvelopeFields_SurfaceAsHeaders()
    {
        var headersGetter = new EventGridMessageHeadersGetter();
        var context = new EventGridContext(EventGridTriggerEvent.Parse("""
        {
            "id": "event-1",
            "topic": "/subscriptions/sub",
            "subject": "some-subject",
            "eventType": "Custom.Event"
        }
        """));

        var headers = headersGetter.GetHeaders(context);

        Assert.Equal("event-1", headers["id"]);
        Assert.Equal("some-subject", headers["subject"]);
        Assert.Equal("/subscriptions/sub", headers["source"]);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task EventWithoutData_BindsEmptyBody()
    {
        var bodyGetter = new EventGridMessageBodyGetter();
        var context = new EventGridContext(EventGridTriggerEvent.Parse("""{ "eventType": "Custom.Event" }"""));

        Assert.Equal("{}", bodyGetter.GetBody(context));
        await Task.CompletedTask;
    }
}
