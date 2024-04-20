using Benzene.Abstractions;

namespace Benzene.Tools;

public class MessageBuilder : IMessageBuilder
{
    public IDictionary<string, string> Headers { get; }
    public string Topic { get; }
    public object Message { get; }

    private MessageBuilder(string topic, object message)
    {
        Message = message;
        Topic = topic;
        Headers = new Dictionary<string, string>();
    }

    public static MessageBuilder Create(string topic)
    {
        return new MessageBuilder(topic, null);
    }

    public static MessageBuilder Create(string topic, object message)
    {
        return new MessageBuilder(topic, message);
    }

    public MessageBuilder WithHeader(string key, string value)
    {
        Headers.Add(key, value);
        return this;
    }
}
