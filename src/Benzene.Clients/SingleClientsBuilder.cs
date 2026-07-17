using Benzene.Abstractions.DI;

namespace Benzene.Clients;

/// <summary>
/// Superseded by <see cref="OutboundRoutingBuilder"/>/<c>AddOutboundRouting(...)</c> - "one client"
/// is just the N=1 case of "many" there, so this cardinality split no longer exists. See
/// <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use OutboundRoutingBuilder/AddOutboundRouting instead - see work/benzene-clients-redesign-plan.md")]
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
