using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace BenzeneStarter;

// This is a starter handler - replace it with your own, or add more alongside it.
//
// Event Grid is different from the queue transports: it routes each event to a handler by its EVENT
// TYPE, not a "topic" header. The value in [Message(...)] must match the event's `eventType`/`type`
// field, and the handler receives the event's `data` payload deserialized to its request type. This
// example handles a custom "hello.world" event type; swap it for a real one (e.g.
// "Microsoft.Storage.BlobCreated") to react to Azure resource events. Fire-and-forget: Event Grid
// handles its own retry/dead-lettering on the subscription, so there's no response to return.
[Message("hello.world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage>
{
    public Task HandleAsync(HelloWorldMessage message)
    {
        Console.WriteLine($"Hello {message.Name}!");
        return Task.CompletedTask;
    }
}

public class HelloWorldMessage
{
    public string Name { get; set; } = string.Empty;
}
