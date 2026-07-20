using System.Net;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Http
{
    /// <summary>
    /// Checks a downstream HTTP dependency by issuing a GET request and treating a
    /// <see cref="HttpStatusCode.OK"/> response as healthy - any other status code (including
    /// non-2xx success codes) is reported unhealthy. Uses whatever timeout/retry policy is
    /// configured on the injected <see cref="HttpClient"/> (e.g. via <c>IHttpClientFactory</c>
    /// named/typed client configuration) - this type does not implement its own timeout or retry
    /// logic.
    /// </summary>
    public class HttpPingHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpPingHealthCheck"/> class.
        /// </summary>
        /// <param name="httpClient">The client used to issue the ping request.</param>
        /// <param name="url">The URL to GET.</param>
        public HttpPingHealthCheck(HttpClient httpClient, string url)
        {
            _url = url;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Issues a GET request to the configured URL and reports healthy only for a 200 OK
        /// response. The result's <see cref="IHealthCheckResult.Data"/> always includes the
        /// checked <c>Url</c> and the response's <c>StatusCode</c>.
        /// </summary>
        public async Task<IHealthCheckResult> ExecuteAsync()
        {
            var dependencies = new[] { new HealthCheckDependency("Http", _url) };

            using var response = await _httpClient.GetAsync(_url);
            return HealthCheckResult.CreateInstance(response.StatusCode == HttpStatusCode.OK, Type,
                new Dictionary<string, object> { { "Url", _url }, { "StatusCode", response.StatusCode } }, dependencies);
        }

        /// <inheritdoc />
        public string Type => "HttpPing";
    }
}
