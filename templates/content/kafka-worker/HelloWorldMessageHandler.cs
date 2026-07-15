using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace BenzeneStarter;

// This is a starter handler - replace it with your own, or add more alongside it.
//
// This is a fire-and-forget handler (IMessageHandler<HelloWorldMessage>, no response type) -
// the right shape for a Kafka record, since nothing is written back to the broker. The Kafka
// consumer routes each record to a handler by matching the record's LITERAL Kafka topic name
// against [Message("...")] - there's no colon-separated topic-id convention here the way there is
// for HTTP/SQS/SNS, so whatever you pass in [Message(...)] must be exactly the Kafka topic string.
[Message("hello_world")]
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
