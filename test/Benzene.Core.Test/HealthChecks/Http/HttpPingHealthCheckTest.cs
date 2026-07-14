using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.Http;
using Xunit;

namespace Benzene.Test.HealthChecks.Http;

public class HttpPingHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_ReturnsHealthy_WhenResponseIsOk()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK));
        var healthCheck = new HttpPingHealthCheck(httpClient, "https://example.test/ping");

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("HttpPing", result.Type);
        Assert.Equal("https://example.test/ping", result.Data["Url"]);
        Assert.Equal(HttpStatusCode.OK, result.Data["StatusCode"]);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task ExecuteAsync_ReturnsFailed_WhenResponseIsNotOk(HttpStatusCode statusCode)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode));
        var healthCheck = new HttpPingHealthCheck(httpClient, "https://example.test/ping");

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(statusCode, result.Data["StatusCode"]);
    }

    [Fact]
    public void Type_IsHttpPing()
    {
        var healthCheck = new HttpPingHealthCheck(new HttpClient(), "https://example.test/ping");

        Assert.Equal("HttpPing", healthCheck.Type);
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public StubHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
