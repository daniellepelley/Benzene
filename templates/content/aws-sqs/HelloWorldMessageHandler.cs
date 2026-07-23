using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace BenzeneStarter;

// This is a starter handler - replace it with your own, or add more alongside it.
//
// This is a fire-and-forget handler (IMessageHandler<HelloWorldMessage>, no response type) - the right
// shape for a queue, topic, or stream consumer, since nothing is written back. Benzene routes each
// incoming message to a handler by matching its topic against [Message("...")]. The handler takes its
// collaborators (here IGreeter) via the constructor - they're resolved from DI, and a test can swap
// them for a stand-in (see BenzeneStarter.Tests). Keeping side effects behind an injected service like
// this is what makes the handler straightforward to test.
[Message("hello:world")]
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
