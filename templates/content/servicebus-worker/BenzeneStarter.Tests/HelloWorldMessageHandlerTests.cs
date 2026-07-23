using Benzene.Azure.ServiceBus.TestHelpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BenzeneStarter.Tests;

// A component test: it boots the SAME app your StartUp configures for a real deployment - both
// ConfigureServices and Configure run - then pushes a message through the whole Benzene pipeline.
// Only the Service Bus broker is simulated (no connection is opened). WithServices/WithConfiguration
// are the seams for overriding any dependency or setting the test needs (here we swap the real
// IGreeter for a spy so we can assert the handler ran, and supply a placeholder connection string so
// the StartUp's ServiceBusClient can be constructed without a real namespace).
public class HelloWorldMessageHandlerTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==";

    private sealed class SpyGreeter : IGreeter
    {
        public List<string> Greeted { get; } = new();
        public void Greet(string name) => Greeted.Add(name);
    }

    private static ServiceBusWorkerBenzeneTestHost BuildHost(IGreeter greeter) =>
        BenzeneTestHost.Create<StartUp>()
            .WithServices(services => services.AddSingleton<IGreeter>(greeter))
            .WithConfiguration("ServiceBus:ConnectionString", FakeConnectionString)
            .BuildServiceBusWorkerHost();

    [Fact]
    public async Task Routing_the_demo_topic_invokes_the_handler()
    {
        var greeter = new SpyGreeter();
        using var host = BuildHost(greeter);

        await host.HandleAsync(
            MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" }).AsAzureServiceBusMessage());

        Assert.Equal(new[] { "World" }, greeter.Greeted);
    }

    [Fact]
    public async Task An_unknown_topic_does_not_reach_the_handler()
    {
        var greeter = new SpyGreeter();
        using var host = BuildHost(greeter);

        await host.HandleAsync(
            MessageBuilder.Create("does:not-exist", new HelloWorldMessage { Name = "World" }).AsAzureServiceBusMessage());

        Assert.Empty(greeter.Greeted);
    }
}
