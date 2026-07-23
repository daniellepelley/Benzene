using Benzene.Kafka.Core.TestHelpers;
using Benzene.Testing;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BenzeneStarter.Tests;

// A component test: it boots the SAME app your StartUp configures for a real deployment - both
// ConfigureServices and Configure run - then pushes a record through the whole Benzene pipeline.
// Only the Kafka broker is simulated (no connection is opened). WithServices/WithConfiguration are
// the seams for overriding any dependency or setting the test needs (here we swap the real IGreeter
// for a spy so we can assert the handler actually ran with the routed record). Kafka routes on the
// literal record topic, so the builder's topic is the Kafka topic name (matching [Message(...)]).
public class HelloWorldMessageHandlerTests
{
    private sealed class SpyGreeter : IGreeter
    {
        public List<string> Greeted { get; } = new();
        public void Greet(string name) => Greeted.Add(name);
    }

    private static KafkaBenzeneTestHost<Ignore, string> BuildHost(IGreeter greeter) =>
        BenzeneTestHost.Create<StartUp>()
            .WithServices(services => services.AddSingleton<IGreeter>(greeter))
            .BuildKafkaWorkerHost<StartUp, Ignore, string>();

    [Fact]
    public async Task Routing_the_demo_topic_invokes_the_handler()
    {
        var greeter = new SpyGreeter();
        using var host = BuildHost(greeter);

        await host.HandleAsync(
            MessageBuilder.Create("hello_world", new HelloWorldMessage { Name = "World" }).AsKafkaBenzeneMessage());

        Assert.Equal(new[] { "World" }, greeter.Greeted);
    }

    [Fact]
    public async Task An_unknown_topic_does_not_reach_the_handler()
    {
        var greeter = new SpyGreeter();
        using var host = BuildHost(greeter);

        await host.HandleAsync(
            MessageBuilder.Create("does_not_exist", new HelloWorldMessage { Name = "World" }).AsKafkaBenzeneMessage());

        Assert.Empty(greeter.Greeted);
    }
}
