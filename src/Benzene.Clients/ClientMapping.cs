using Benzene.Abstractions.DI;

namespace Benzene.Clients;

/// <summary>
/// Part of the obsolete <see cref="ClientsBuilder"/> mechanism, superseded by
/// <see cref="OutboundRoutingBuilder"/>. See <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use OutboundRoutingBuilder/AddOutboundRouting instead - see work/benzene-clients-redesign-plan.md")]
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
