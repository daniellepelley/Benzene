using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Tracing.Tempo;
using Xunit;

namespace Benzene.Mesh.Test;

public class PrometheusQueryClientTest
{
    private const string PrometheusUrl = "https://prometheus.example/api/v1/query";

    private class FixedResponseHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public FixedResponseHttpMessageHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode) { Content = new StringContent(_body) });
        }
    }

    private static PrometheusQueryClient CreateClient(HttpStatusCode statusCode, string body)
    {
        return new PrometheusQueryClient(new HttpClient(new FixedResponseHttpMessageHandler(statusCode, body)));
    }

    [Fact]
    public async Task QueryAsync_SuccessfulVectorResult_ReturnsSamples()
    {
        var body = """
        {
          "status": "success",
          "data": {
            "resultType": "vector",
            "result": [
              { "metric": { "client": "orders-api", "server": "payments-api" }, "value": [1700000000, "42.5"] },
              { "metric": { "client": "orders-api", "server": "shipping-api" }, "value": [1700000000, "3"] }
            ]
          }
        }
        """;
        var client = CreateClient(HttpStatusCode.OK, body);

        var samples = await client.QueryAsync(PrometheusUrl, "sum by (client, server) (rate(traces_service_graph_request_total[5m]))");

        Assert.Equal(2, samples.Count);
        Assert.Equal("orders-api", samples[0].Labels["client"]);
        Assert.Equal("payments-api", samples[0].Labels["server"]);
        Assert.Equal(42.5, samples[0].Value);
        Assert.Equal(3, samples[1].Value);
    }

    [Fact]
    public async Task QueryAsync_EmptyResult_ReturnsEmpty()
    {
        var body = """{ "status": "success", "data": { "resultType": "vector", "result": [] } }""";
        var client = CreateClient(HttpStatusCode.OK, body);

        var samples = await client.QueryAsync(PrometheusUrl, "up");

        Assert.Empty(samples);
    }

    [Fact]
    public async Task QueryAsync_StatusError_ReturnsEmpty()
    {
        var body = """{ "status": "error", "errorType": "bad_data", "error": "invalid parameter \"query\"" }""";
        var client = CreateClient(HttpStatusCode.OK, body);

        var samples = await client.QueryAsync(PrometheusUrl, "not a valid promql (((");

        Assert.Empty(samples);
    }

    [Fact]
    public async Task QueryAsync_MalformedJson_ReturnsEmpty()
    {
        var client = CreateClient(HttpStatusCode.OK, "<html>not json</html>");

        var samples = await client.QueryAsync(PrometheusUrl, "up");

        Assert.Empty(samples);
    }

    [Fact]
    public async Task QueryAsync_HttpErrorStatus_ReturnsEmpty()
    {
        var client = CreateClient(HttpStatusCode.ServiceUnavailable, "");

        var samples = await client.QueryAsync(PrometheusUrl, "up");

        Assert.Empty(samples);
    }

    [Theory]
    // Valid JSON but a structurally-unexpected shape: the strongly-typed JsonElement accessors throw
    // InvalidOperationException (not JsonException), which the documented "malformed/unexpected body ->
    // empty" contract must still swallow rather than fault the whole topology build.
    [InlineData("{ \"status\": 1 }")]                                                                    // status not a string -> GetString()
    [InlineData("{ \"status\": \"success\", \"data\": { \"result\": [ { \"metric\": {}, \"value\": \"oops\" } ] } }")] // value not an array -> GetArrayLength()
    [InlineData("{ \"status\": \"success\", \"data\": { \"result\": [ { \"metric\": {}, \"value\": [1700000000, 42.5] } ] } }")] // numeric value element -> GetString()
    public async Task QueryAsync_ValidJsonUnexpectedShape_ReturnsEmpty(string body)
    {
        var client = CreateClient(HttpStatusCode.OK, body);

        var samples = await client.QueryAsync(PrometheusUrl, "up");

        Assert.Empty(samples);
    }
}
