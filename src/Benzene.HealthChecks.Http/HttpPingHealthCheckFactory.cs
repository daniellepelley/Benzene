using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Http;

public class HttpPingHealthCheckFactory : IHealthCheckFactory
{
    private readonly string _url;

    public HttpPingHealthCheckFactory(string url)
    {
        _url = url;
    }

    public IHealthCheck Create(IServiceResolver resolver)
    {
        var httpClient = resolver.GetService<HttpClient>();
        return new HttpPingHealthCheck(httpClient, _url);
    }
}
