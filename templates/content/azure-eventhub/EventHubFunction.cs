using Azure.Messaging.EventHubs;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventHub.Function;
using Microsoft.Azure.Functions.Worker;

namespace BenzeneStarter;

// One Event Hub trigger hands each batch of events to Benzene, which routes every event to a handler.
// You add message handlers, not new Functions.
public class EventHubFunction
{
    private readonly IAzureFunctionApp _app;

    public EventHubFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("event-hub")]
    public Task Run(
        [EventHubTrigger("hello_world", Connection = "EventHubConnection")] EventData[] events)
    {
        return _app.HandleEventHub(events);
    }
}
