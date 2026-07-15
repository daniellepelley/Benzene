using System.Net;
using System.Net.Http;
using System.Text.Json;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Reporting;
using Xunit;

namespace Benzene.Mesh.Test;

public class HttpMeshReportPublisherTest
{
    [Fact]
    public async Task PublishAsync_PostsReportJsonToIngestionUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new CapturingHttpMessageHandler((request, body) =>
        {
            capturedRequest = request;
            capturedBody = body;
        });
        var publisher = new HttpMeshReportPublisher(new HttpClient(handler), new MeshReportingOptions("https://mesh.internal/mesh/report"));
        var report = new MeshServiceReport("payments-fn", DateTimeOffset.UtcNow, "{\"info\":{}}", null, null);

        await publisher.PublishAsync(report);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://mesh.internal/mesh/report", capturedRequest.RequestUri!.ToString());
        Assert.NotNull(capturedBody);

        var deserialized = JsonSerializer.Deserialize<MeshServiceReport>(capturedBody!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("payments-fn", deserialized!.Name);
    }

    [Fact]
    public async Task PublishAsync_NonSuccessResponse_Throws()
    {
        var handler = new CapturingHttpMessageHandler((_, _) => { }, HttpStatusCode.InternalServerError);
        var publisher = new HttpMeshReportPublisher(new HttpClient(handler), new MeshReportingOptions("https://mesh.internal/mesh/report"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            publisher.PublishAsync(new MeshServiceReport("payments-fn", DateTimeOffset.UtcNow, null, null, null)));
    }

    private class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Action<HttpRequestMessage, string> _capture;
        private readonly HttpStatusCode _statusCode;

        public CapturingHttpMessageHandler(Action<HttpRequestMessage, string> capture, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _capture = capture;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            _capture(request, body);
            return new HttpResponseMessage(_statusCode);
        }
    }
}
