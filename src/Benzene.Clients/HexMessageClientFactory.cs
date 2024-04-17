using Benzene.Abstractions.DI;

namespace Benzene.Clients;

public class BenzeneMessageClientFactory : IBenzeneMessageClientFactory
{
    private readonly Dictionary<ClientMapping, IBenzeneMessageClient> _clients = new();

    public BenzeneMessageClientFactory(IEnumerable<ClientMapping> clientMapping, IServiceResolver serviceResolver)
    {
        _clients = clientMapping.ToDictionary(x => x,
            x => x.Builder(serviceResolver));
    }

    public IBenzeneMessageClient Create()
    {
        return _clients.First().Value;
    }

    public IBenzeneMessageClient Create(string service, string topic)
    {
        var match = _clients
            .FirstOrDefault(x => Contains(service, topic, x.Key));

        if (match.Value == null)
        {
            throw new InvalidOperationException($"There is no IBenzeneMessageClient registered for service {service} and topic {topic}.");
        }
        
        return match.Value;
    }

    private static bool Contains(string service, string topic, ClientMapping clientMapping)
    {
        if (!string.IsNullOrEmpty(service) && !string.IsNullOrEmpty(topic))
        {
            return clientMapping.Keys.Any(x => x.Service == service && x.Topic == topic)
                   || clientMapping.Keys.Any(x => x.Service == service && string.IsNullOrEmpty(x.Topic))
                   || clientMapping.Keys.Any(x => x.Topic == topic && string.IsNullOrEmpty(x.Service));
        }

        if (!string.IsNullOrEmpty(service) && string.IsNullOrEmpty(topic))
        {
            return clientMapping.Keys.Any(x => x.Service == service && string.IsNullOrEmpty(x.Topic));
        }

        if (!string.IsNullOrEmpty(topic) && string.IsNullOrEmpty(service))
        {
            return clientMapping.Keys.Any(x => x.Topic == topic && string.IsNullOrEmpty(x.Service));
        }

        return false;
    }
}
