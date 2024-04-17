namespace Benzene.Tools;

[Obsolete("Use MessageBuilder instead", false)]
public class EventBuilder : MessageBuilder
{
    protected EventBuilder(string topic, object message) : base(topic, message)
    {
    }
}
