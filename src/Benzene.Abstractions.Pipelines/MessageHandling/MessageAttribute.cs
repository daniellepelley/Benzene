namespace Benzene.Abstractions.MessageHandling;


[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MessageAttribute : Attribute
{
    public MessageAttribute(string topic, string version = "")
    {
        Version = version;
        Topic = topic;
    }

    public string Version { get; }
    public string Topic { get; }
}
