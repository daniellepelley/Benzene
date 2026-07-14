using System;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// Terminal send-pipeline middleware that publishes the built <c>PutEvents</c> request via the
/// injected EventBridge client. Failures propagate — callers (e.g.
/// <see cref="EventBridgeBenzeneMessageClient"/>) map them to a Benzene result.
/// </summary>
public class EventBridgeClientMiddleware : IMiddleware<EventBridgeSendMessageContext>
{
    private readonly IAmazonEventBridge _amazonEventBridge;

    public EventBridgeClientMiddleware(IAmazonEventBridge amazonEventBridge)
    {
        _amazonEventBridge = amazonEventBridge;
    }

    public string Name => nameof(EventBridgeClientMiddleware);

    public async Task HandleAsync(EventBridgeSendMessageContext context, Func<Task> next)
    {
        context.Response = await _amazonEventBridge.PutEventsAsync(context.Request);
    }
}
