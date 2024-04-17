namespace Benzene.Clients;

public class TopicAndServiceKey
{
    public string Topic { get; }
    public string Service { get; }

    public TopicAndServiceKey(string topic, string service)
    {
        Topic = topic;
        Service = service;
    }
}