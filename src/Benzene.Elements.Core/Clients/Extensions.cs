using Benzene.Clients;

namespace Benzene.Elements.Core.Clients;

public static class Extensions
{
    public static ClientBuilder WithCorrelationId(this ClientBuilder source)
    {
        return source.WithDependencyWrapper(new CorrelationIdBenzeneMessageClientWrapper());
    }

}
