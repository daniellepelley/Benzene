using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Http;

/// <summary>
/// Builds an <see cref="HttpPingHealthCheck"/> for a fixed URL, resolving the <see cref="HttpClient"/>
/// it pings with from DI each time the check runs (rather than capturing one at registration time).
/// </summary>
public class HttpPingHealthCheckFactory : IHealthCheckFactory
{
    private readonly string _url;

    /// <summary>Initializes a new instance of the <see cref="HttpPingHealthCheckFactory"/> class.</summary>
    /// <param name="url">The URL the resulting health check will GET.</param>
    public HttpPingHealthCheckFactory(string url)
    {
        _url = url;
    }

    /// <inheritdoc />
    public IHealthCheck Create(IServiceResolver resolver)
    {
        var httpClient = resolver.GetService<HttpClient>();
        return new HttpPingHealthCheck(httpClient, _url);
    }
}
