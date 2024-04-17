namespace Benzene.Clients;

public static class Extensions
{
    public static ClientBuilder WithRetry(this ClientBuilder source, int numberOfRetries)
    {
        return source.WithDependencyWrapper(new RetryBenzeneMessageClientWrapper(numberOfRetries));
    }
}
