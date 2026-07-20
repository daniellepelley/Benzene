using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Http;
using Benzene.Http.Cors;
using Benzene.Http.Routing;
using Xunit;

namespace Benzene.Test.Core.Http;

public class CorsMiddlewareOrderingTest
{
    private sealed class FakeHttpContext : IHttpContext { }

    private sealed class FakeRequestAdapter : IHttpRequestAdapter<FakeHttpContext>
    {
        private readonly HttpRequest _request;
        public FakeRequestAdapter(HttpRequest request) => _request = request;
        public HttpRequest Map(FakeHttpContext context) => _request;
    }

    private sealed class FakeEndpointFinder : IHttpEndpointFinder
    {
        private readonly IHttpEndpointDefinition[] _definitions;
        public FakeEndpointFinder(params IHttpEndpointDefinition[] definitions) => _definitions = definitions;
        public IHttpEndpointDefinition[] FindDefinitions() => _definitions;
    }

    // Simulates a real-server response (ASP.NET/self-host): once the response has been finalized
    // (flushed), setting a header throws - which is exactly the failure the ordering fix prevents.
    private sealed class RealServerResponseAdapter : IBenzeneResponseAdapter<FakeHttpContext>
    {
        public Dictionary<string, string> Headers { get; } = new();
        public bool Finalized { get; set; }

        public void SetResponseHeader(FakeHttpContext context, string headerKey, string headerValue)
        {
            if (Finalized)
            {
                throw new InvalidOperationException("Headers are read-only, response has already started.");
            }

            Headers[headerKey] = headerValue;
        }

        public void SetContentType(FakeHttpContext context, string contentType) { }
        public void SetStatusCode(FakeHttpContext context, string statusCode) { }
        public void SetBody(FakeHttpContext context, string body) { }
        public string GetBody(FakeHttpContext context) => string.Empty;
        public Task FinalizeAsync(FakeHttpContext context) => Task.CompletedTask;
    }

    private sealed class FakeResultSetter : IMessageHandlerResultSetter<FakeHttpContext>
    {
        public Task SetResultAsync(FakeHttpContext context, IMessageHandlerResult messageHandlerResult) => Task.CompletedTask;
    }

    [Fact]
    public async Task ActualRequest_SetsCorsHeadersBeforeTheResponseIsFinalized()
    {
        var request = new HttpRequest
        {
            Method = "GET",
            Path = "/thing",
            Headers = new Dictionary<string, string> { ["origin"] = "https://example.com" }
        };
        var responseAdapter = new RealServerResponseAdapter();
        var cors = new CorsMiddleware<FakeHttpContext>(
            new CorsSettings { AllowedDomains = new[] { "*" }, AllowedHeaders = new[] { "*" } },
            new FakeEndpointFinder(new HttpEndpointDefinition("get", "/thing", "topic")),
            new FakeRequestAdapter(request),
            responseAdapter,
            new FakeResultSetter());

        // next() represents the handler running and finalizing (flushing) the response, after which
        // headers are read-only. The CORS headers must be set before this - previously they were set
        // AFTER next(), throwing on real servers.
        await cors.HandleAsync(new FakeHttpContext(), () =>
        {
            responseAdapter.Finalized = true;
            return Task.CompletedTask;
        });

        Assert.True(responseAdapter.Headers.ContainsKey("Access-Control-Allow-Origin"));
        Assert.Equal("https://example.com", responseAdapter.Headers["Access-Control-Allow-Origin"]);
        Assert.True(responseAdapter.Headers.ContainsKey("Vary"));
    }
}
