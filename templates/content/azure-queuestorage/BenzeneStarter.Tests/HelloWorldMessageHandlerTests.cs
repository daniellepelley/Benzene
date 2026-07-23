using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Core.TestHelpers;
using Benzene.Azure.Function.QueueStorage;
using Benzene.Azure.Function.QueueStorage.TestHelpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BenzeneStarter.Tests;

// A component test: it boots the SAME app your StartUp configures for a real deployment - both
// ConfigureServices and Configure run - then pushes a message through the whole Benzene pipeline.
// Only the Queue Storage trigger is simulated. WithServices/WithConfiguration are the seams for
// overriding any dependency or setting the test needs (here we swap the real IGreeter for a spy so we
// can assert the handler actually ran with the routed message). A Queue Storage message carries no
// properties, so the topic rides in a Benzene envelope in the message text (AsQueueStorageBenzeneMessage).
public class HelloWorldMessageHandlerTests
{
    private sealed class SpyGreeter : IGreeter
    {
        public List<string> Greeted { get; } = new();
        public void Greet(string name) => Greeted.Add(name);
    }

    private static IAzureFunctionApp BuildApp(IGreeter greeter) =>
        BenzeneTestHost.Create<StartUp>()
            .WithServices(services => services.AddSingleton<IGreeter>(greeter))
            .BuildAzureFunctionApp();

    [Fact]
    public async Task Routing_the_demo_topic_invokes_the_handler()
    {
        var greeter = new SpyGreeter();
        var app = BuildApp(greeter);

        await app.HandleQueueMessages(
            MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" }).AsQueueStorageBenzeneMessage());

        Assert.Equal(new[] { "World" }, greeter.Greeted);
    }

    [Fact]
    public async Task An_unknown_topic_does_not_reach_the_handler()
    {
        var greeter = new SpyGreeter();
        var app = BuildApp(greeter);

        await app.HandleQueueMessages(
            MessageBuilder.Create("does:not-exist", new HelloWorldMessage { Name = "World" }).AsQueueStorageBenzeneMessage());

        Assert.Empty(greeter.Greeted);
    }
}
