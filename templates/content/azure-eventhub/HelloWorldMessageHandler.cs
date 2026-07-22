using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace BenzeneStarter;

// This is a starter handler - replace it with your own, or add more alongside it.
//
// This is a fire-and-forget handler (IMessageHandler<HelloWorldMessage>, no response type) - the
// right shape for a queue/topic consumer, since nothing is written back to the broker. Benzene routes
// each incoming message to a handler by matching the message's topic against [Message("...")]: for
// these transports the topic travels as the message's "topic" header/property (or, for the
// envelope-based Azure triggers, the "topic" field of the Benzene message envelope). A successful
// handler acks the message; a thrown exception abandons it (with the Explicit ack mode these
// templates use), so the broker can redeliver.
[Message("hello:world")]
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
