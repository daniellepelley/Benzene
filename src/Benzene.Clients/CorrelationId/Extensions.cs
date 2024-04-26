namespace Benzene.Clients.CorrelationId;

public static class Extensions
{
    public static ClientBuilder WithCorrelationId(this ClientBuilder source)
    {
        return source.WithDependencyWrapper(new CorrelationIdBenzeneMessageClientWrapper());
    }

}
