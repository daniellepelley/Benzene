using Benzene.Abstractions.DI;

namespace Benzene.Clients;

public class SingleClientsBuilder
{
    private readonly List<Func<IServiceResolver, IBenzeneMessageClient>> _builders = new();
    public void Register(IBenzeneServiceContainer benzeneServiceContainer)
    {
        foreach (var builder in _builders)
        {
            benzeneServiceContainer.AddScoped<IBenzeneMessageClient>(builder);
        }
    }
    public void WithMessageClient(Func<IServiceResolver, IBenzeneMessageClient> builder)
    {
        _builders.Add(builder);
    }
}
