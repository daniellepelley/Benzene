using System.Net;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Http
{
    public class HttpPingHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;

        public HttpPingHealthCheck(HttpClient httpClient, string url)
        {
            _url = url;
            _httpClient = httpClient;
        }

        public async Task<IHealthCheckResult> ExecuteAsync()
        {
            var response = await _httpClient.GetAsync(_url);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return HealthCheckResult.CreateInstance(true, Type,
                    new Dictionary<string, object> { { "Url", _url }, { "StatusCode", response.StatusCode } });
            }

            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "Url", _url }, { "StatusCode", response.StatusCode } });
        }

        public string Type => "HttpPing";
    }
}
