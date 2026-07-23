using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Client.Http;
using Benzene.Clients;
using Benzene.Results;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Http;

/// <summary>
/// Coverage for <see cref="HttpBenzeneMessageClient"/> - the client that carries a BenzeneMessage envelope
/// (<c>{ topic, headers, body }</c>) over HTTP to another Benzene service's <c>/benzene-message</c> endpoint,
/// the HTTP counterpart of the Lambda invoke path.
/// </summary>
public class HttpBenzeneMessageClientTest
{
    private const string Url = "https://service-b.internal/benzene-message";

    [Fact]
    public async Task SendMessageAsync_PostsTheEnvelope_ToTheConfiguredUrl_WithTopicHeadersAndSerializedBody()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, """{"statusCode":"ok","headers":{},"body":"\"hello\""}""");
        var client = new HttpBenzeneMessageClient(new HttpClient(handler), Url);

        var request = new BenzeneClientRequest<string>("my-topic", "the-message",
            new Dictionary<string, string> { { "tenant", "acme" } });

        await client.SendMessageAsync<string, string>(request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(Url, handler.LastRequest.RequestUri!.ToString());

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("my-topic", doc.RootElement.GetProperty("topic").GetString());
        Assert.Equal("acme", doc.RootElement.GetProperty("headers").GetProperty("tenant").GetString());
        // The body is the serialized message payload (a JSON string here), nested inside the envelope.
        Assert.Equal("\"the-message\"", doc.RootElement.GetProperty("body").GetString());
    }

    [Fact]
    public async Task SendMessageAsync_MapsASuccessEnvelope_ToAnOkResultWithThePayload()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, """{"statusCode":"ok","headers":{},"body":"\"hello\""}""");
        var client = new HttpBenzeneMessageClient(new HttpClient(handler), Url);

        var result = await client.SendMessageAsync<string, string>(
            new BenzeneClientRequest<string>("t", "m", new Dictionary<string, string>()));

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
        Assert.Equal("hello", result.Payload);
    }

    [Fact]
    public async Task SendMessageAsync_MapsTheEnvelopeStatus_EvenWhenTheHttpStatusIsNon2xx()
    {
        // The target maps NotFound onto HTTP 404, but the authoritative status is in the envelope body -
        // a mapped non-2xx is a normal result, not a transport failure, so we must not throw on it.
        var handler = new CapturingHandler(HttpStatusCode.NotFound, """{"statusCode":"not-found","headers":{},"body":null}""");
        var client = new HttpBenzeneMessageClient(new HttpClient(handler), Url);

        var result = await client.SendMessageAsync<string, string>(
            new BenzeneClientRequest<string>("t", "m", new Dictionary<string, string>()));

        Assert.Equal(BenzeneResultStatus.NotFound, result.Status);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task SendMessageAsync_ForVoidResponse_MapsStatusWithoutDeserializingABody()
    {
        // A Void (send-acknowledgement) caller: the handler's body shape is undefined, so we map the status
        // only. A non-envelope-shaped body must not break the Void path.
        var handler = new CapturingHandler(HttpStatusCode.OK, """{"statusCode":"ok","headers":{},"body":"anything at all"}""");
        var client = new HttpBenzeneMessageClient(new HttpClient(handler), Url);

        var result = await client.SendMessageAsync<string, Void>(
            new BenzeneClientRequest<string>("t", "m", new Dictionary<string, string>()));

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsServiceUnavailable_WhenTheTransportThrows()
    {
        var client = new HttpBenzeneMessageClient(new HttpClient(new ThrowingHandler()), Url);

        var result = await client.SendMessageAsync<string, string>(
            new BenzeneClientRequest<string>("t", "m", new Dictionary<string, string>()));

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsServiceUnavailable_OnAnEmptyResponseBody()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "");
        var client = new HttpBenzeneMessageClient(new HttpClient(handler), Url);

        var result = await client.SendMessageAsync<string, string>(
            new BenzeneClientRequest<string>("t", "m", new Dictionary<string, string>()));

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public HttpRequestMessage? LastRequest;
        public string? LastRequestBody;

        public CapturingHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequest = request;
            LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("connection refused");
    }
}
