namespace Benzene.Clients;

public class ClientMappingBuilder
{
    private readonly List<TopicAndServiceKey> _topicsAndServiceKeys = new();

    public ClientMappingBuilder ForService(params string[] services)
    {
        AddRange(services.Select(service => new TopicAndServiceKey(string.Empty, service)));
        return this;
    }

    public ClientMappingBuilder ForTopic(params string[] topics)
    {
        AddRange(topics.Select(topic => new TopicAndServiceKey(topic, string.Empty)));
        return this;
    }

    public ClientMappingBuilder ForServiceAndTopic(string service, string topic)
    {
        AddRange(new[] { new TopicAndServiceKey(topic, service)});
        return this;
    }
    
    public ClientMappingBuilder ForTopicAndService(string topic, params string[] services)
    {
        AddRange(services.Select(service => new TopicAndServiceKey(topic, service)));
        return this;
    }

    public TopicAndServiceKey[] Build()
    {
        return _topicsAndServiceKeys
            .ToArray();
    }

    private void AddRange(IEnumerable<TopicAndServiceKey> topicAndServiceKeys)
    {
        foreach (var topicAndServiceKey in topicAndServiceKeys)
        {
            if (_topicsAndServiceKeys.Any(x =>
                    x.Topic == topicAndServiceKey.Topic && x.Service == topicAndServiceKey.Service))
            {
                throw new ArgumentException("Duplicate client mapping");
            }   
            _topicsAndServiceKeys.Add(topicAndServiceKey);
        }
    }
}
