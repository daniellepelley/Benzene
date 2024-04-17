using Benzene.Abstractions.DI;

namespace Benzene.Clients;

public class ClientMapping
{
    public TopicAndServiceKey[] Keys { get; }
    public Func<IServiceResolver, IBenzeneMessageClient> Builder { get; }
    public ClientMapping(string topic, string service, Func<IServiceResolver, IBenzeneMessageClient> builder)
        : this(new[] { new TopicAndServiceKey(topic, service) }, builder)
    { }

    public ClientMapping(TopicAndServiceKey[] keys, Func<IServiceResolver, IBenzeneMessageClient> builder)
    {
        Keys = keys;
        Builder = builder;
    }
}
