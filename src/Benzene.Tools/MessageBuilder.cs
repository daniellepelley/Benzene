using Benzene.Abstractions;

namespace Benzene.Tools;

public class MessageBuilder<T> : IMessageBuilder<T>
{
    public IDictionary<string, string> Headers { get; }
    public string Topic { get; }
    public T? Message { get; }

    internal MessageBuilder(string topic, T message)
    {
        Message = message;
        Topic = topic;
        Headers = new Dictionary<string, string>();
    }

    public MessageBuilder<T> WithHeader(string key, string value)
    {
        Headers.Add(key, value);
        return this;
    }
}

public static class MessageBuilder
{
    public static MessageBuilder<object> Create(string topic)
    {
        return new MessageBuilder<object>(topic, null);
    }

    public static MessageBuilder<T> Create<T>(string topic, T message)
    {
        return new MessageBuilder<T>(topic, message);
    }
}
