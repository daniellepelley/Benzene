namespace Benzene.Clients;

/// <summary>
/// Part of the obsolete <see cref="ClientsBuilder"/> mechanism, superseded by
/// <see cref="OutboundRoutingBuilder"/>. See <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use OutboundRoutingBuilder/AddOutboundRouting instead - see work/benzene-clients-redesign-plan.md")]
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