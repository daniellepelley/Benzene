using Benzene.Abstractions.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BenzeneStarter.Tests;

// These tests route a message through Benzene's transport-agnostic in-memory host (BenzeneMessage),
// exercising your real handler and its routing without spinning up the actual transport (a queue,
// topic, or stream). The same handler answers every transport by its topic, so proving it here proves
// it everywhere. Add a test per handler as your service grows.
public class HelloWorldMessageHandlerTests
{
    // A minimal in-memory Benzene host: it discovers the handlers in the main project and routes a
    // BenzeneMessage (a topic + JSON body) to them - the same routing every Benzene transport performs.
    private static async Task<IBenzeneMessageResponse> SendAsync(string topic, string body)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var container = new MicrosoftBenzeneServiceContainer(services);
        container
            .AddBenzene()
            .AddBenzeneMessage()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly);

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipeline.UseMessageHandlers();

        var app = new BenzeneMessageApplication(pipeline.Build());
        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return await app.HandleAsync(
            new BenzeneMessageRequest { Topic = topic, Headers = new Dictionary<string, string>(), Body = body },
            serviceResolverFactory);
    }

    [Fact]
    public async Task Sending_the_hello_world_topic_is_Accepted()
    {
        // This is a fire-and-forget handler (no response payload), so a successfully-handled message
        // comes back as Accepted - the transport acks it and nothing is written back to the broker.
        var response = await SendAsync("hello:world", "{\"name\":\"World\"}");

        Assert.Equal("Accepted", response.StatusCode);
    }

    [Fact]
    public async Task Sending_an_unknown_topic_returns_NotFound()
    {
        // Nothing handles this topic, so Benzene's router returns a NotFound result - the same
        // behaviour every transport surfaces when a message has no handler.
        var response = await SendAsync("does:not-exist", "{}");

        Assert.Equal("NotFound", response.StatusCode);
    }
}
