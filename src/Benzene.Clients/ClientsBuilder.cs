using Benzene.Abstractions.DI;

namespace Benzene.Clients;

public class ClientsBuilder
{
    private readonly List<ClientMapping> _clientMappings = new();


    public ClientsBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        benzeneServiceContainer.AddScoped<IBenzeneMessageClientFactory>(resolver => new BenzeneMessageClientFactory(_clientMappings.ToArray(), resolver));
    }
    
    // public void Register(IBenzeneServiceContainer benzeneServiceContainer)
    // {
    //     if (_clientMappings.Count == 1)
    //     {
    //         benzeneServiceContainer.AddScoped(_clientMappings.First().Builder);
    //     }
    //
    //     benzeneServiceContainer.AddScoped<IBenzeneMessageClientFactory>(resolver => new BenzeneMessageClientFactory(_clientMappings.ToArray(), resolver));
    // }

    public ClientsBuilder WithMessageClient(ClientMapping clientMapping)
    {
        _clientMappings.Add(clientMapping);

        if (_clientMappings.SelectMany(x => x.Keys)
            .GroupBy(x => new { x.Topic, x.Service })
            .Any(x => x.Count() > 1))
        {
            throw new ArgumentException("Duplicate client mapping");
        }

        return this;
    }

    public ClientsBuilder WithMessageClient(string service, Func<IServiceResolver, IBenzeneMessageClient> builder)
    {
        return WithMessageClient(new ClientMapping(string.Empty, service, builder));
    }
}
