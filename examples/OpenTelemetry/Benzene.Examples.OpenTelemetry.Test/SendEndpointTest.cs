using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Benzene.Examples.OpenTelemetry.Test;

/// <summary>
/// Drives the OpenTelemetry example's real entry point (<c>Program.cs</c>) in-memory via
/// <see cref="WebApplicationFactory{TEntryPoint}"/> - <see cref="GreetingRequest"/> is only used to
/// point the factory at the example's assembly. Proves the demo's <c>/api/send</c> pipeline actually
/// dispatches each handler (including the deliberately-failing one) and that <c>/api/topics</c>
/// reflects the registered handlers, without needing the otel-lgtm collector the example exports to
/// (the OTLP exporter simply has nothing to talk to under test, which never fails a request).
/// </summary>
public class SendEndpointTest : IClassFixture<WebApplicationFactory<GreetingRequest>>
{
    private readonly WebApplicationFactory<GreetingRequest> _factory;

    public SendEndpointTest(WebApplicationFactory<GreetingRequest> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Topics_ListsEveryRegisteredHandler()
    {
        var client = _factory.CreateClient();

        var topics = await client.GetFromJsonAsync<TopicRow[]>("/api/topics");

        Assert.NotNull(topics);
        var ids = topics!.Select(t => t.Topic).ToArray();
        Assert.Contains("greeting", ids);
        Assert.Contains("order_create", ids);
        Assert.Contains("order_fail", ids);
    }

    [Fact]
    public async Task Send_Greeting_DispatchesTheHandlerAndReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/send",
            new { topic = "greeting", body = "{\"name\":\"acme\"}" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SendResult>();
        Assert.Equal("ok", result!.StatusCode);
        Assert.Contains("Hello, acme!", result.Body);
    }

    [Fact]
    public async Task Send_CreateOrder_RunsThroughTheWarehouseServiceAndReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/send",
            new { topic = "order_create", body = "{\"productId\":\"prod-1\",\"quantity\":2}" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SendResult>();
        Assert.Equal("created", result!.StatusCode);
        Assert.Contains("prod-1", result.Body);
    }

    [Fact]
    public async Task Send_FailingOrder_SurfacesTheHandlerFailureStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/send",
            new { topic = "order_fail", body = "{\"reason\":\"card-declined\"}" });

        // The endpoint always returns 200 with the envelope; the Benzene status inside reflects the
        // handler throwing (a thrown handler maps to the transport's generic-error status).
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SendResult>();
        Assert.NotEqual("ok", result!.StatusCode);
        Assert.NotEqual("created", result.StatusCode);
    }

    private record TopicRow(string Topic, string Version, string Handler, string RequestType);

    private record SendResult(string StatusCode, string Body, Dictionary<string, string>? Headers, string? TraceId);
}
