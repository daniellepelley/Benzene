using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventGrid;
using Microsoft.Azure.Functions.Worker;

namespace BenzeneStarter;

// One Event Grid trigger hands each event to Benzene, which routes it to a handler by the event's type.
// You add message handlers, not new Functions.
public class EventGridFunction
{
    private readonly IAzureFunctionApp _app;

    public EventGridFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("event-grid")]
    public Task Run([EventGridTrigger] string eventJson)
    {
        return _app.HandleEventGridEvent(eventJson);
    }
}
