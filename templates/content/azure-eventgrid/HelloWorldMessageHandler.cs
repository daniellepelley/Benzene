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
// handles its own retry/dead-lettering on the subscription, so there's no response to return. The
// handler takes its collaborators (here IGreeter) via the constructor - they're resolved from DI, and
// a test can swap them for a stand-in (see BenzeneStarter.Tests).
[Message("hello.world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage>
{
    private readonly IGreeter _greeter;

    public HelloWorldMessageHandler(IGreeter greeter)
    {
        _greeter = greeter;
    }

    public Task HandleAsync(HelloWorldMessage message)
    {
        _greeter.Greet(message.Name);
        return Task.CompletedTask;
    }
}

public class HelloWorldMessage
{
    public string Name { get; set; } = string.Empty;
}

// A tiny example dependency, registered in StartUp and injected into the handler above. It exists to
// show the shape - a handler depends on an interface, the app registers a real implementation, and a
// test overrides it. Replace it with your own services (a repository, an HTTP client, ...).
public interface IGreeter
{
    void Greet(string name);
}

public class ConsoleGreeter : IGreeter
{
    public void Greet(string name) => Console.WriteLine($"Hello {name}!");
}
