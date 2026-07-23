using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BenzeneStarter.Tests;

// A component test: it boots the SAME app your StartUp configures for a real deployment - both
// ConfigureServices and Configure run - then pushes a message through the whole Benzene pipeline.
// Only the SQS trigger is simulated. WithServices/WithConfiguration are the seams for overriding any
// dependency or setting the test needs (here we swap the real IGreeter for a spy so we can assert the
// handler actually ran with the routed message).
public class HelloWorldMessageHandlerTests
{
    private sealed class SpyGreeter : IGreeter
    {
        public List<string> Greeted { get; } = new();
        public void Greet(string name) => Greeted.Add(name);
    }

    private static AwsLambdaBenzeneTestHost BuildHost(IGreeter greeter) =>
        new(BenzeneTestHost.Create<StartUp>()
            .WithServices(services => services.AddSingleton<IGreeter>(greeter))
            .WithConfiguration("Example:Setting", "test-value")
            .BuildAwsLambdaHost());

    [Fact]
    public async Task Routing_the_demo_topic_invokes_the_handler()
    {
        var greeter = new SpyGreeter();
        using var host = BuildHost(greeter);

        await host.SendSqsAsync(MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" }));

        Assert.Equal(new[] { "World" }, greeter.Greeted);
    }

    [Fact]
    public async Task An_unknown_topic_does_not_reach_the_handler()
    {
        var greeter = new SpyGreeter();
        using var host = BuildHost(greeter);

        await host.SendSqsAsync(MessageBuilder.Create("does:not-exist", new HelloWorldMessage { Name = "World" }));

        Assert.Empty(greeter.Greeted);
    }
}
