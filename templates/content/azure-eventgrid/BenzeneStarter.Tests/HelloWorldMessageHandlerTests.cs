using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Core.TestHelpers;
using Benzene.Azure.Function.EventGrid;
using Benzene.Azure.Function.EventGrid.TestHelpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BenzeneStarter.Tests;

// A component test: it boots the SAME app your StartUp configures for a real deployment - both
// ConfigureServices and Configure run - then pushes an event through the whole Benzene pipeline.
// Only the Event Grid trigger is simulated. WithServices/WithConfiguration are the seams for
// overriding any dependency or setting the test needs (here we swap the real IGreeter for a spy so we
// can assert the handler actually ran with the routed event). Event Grid routes by event TYPE, so the
// builder's topic becomes the event type (AsEventGridBenzeneMessage).
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
    public async Task Routing_the_demo_event_type_invokes_the_handler()
    {
        var greeter = new SpyGreeter();
        var app = BuildApp(greeter);

        await app.HandleEventGridEvents(
            MessageBuilder.Create("hello.world", new HelloWorldMessage { Name = "World" }).AsEventGridBenzeneMessage());

        Assert.Equal(new[] { "World" }, greeter.Greeted);
    }

    [Fact]
    public async Task An_unknown_event_type_is_raised_for_retry()
    {
        var greeter = new SpyGreeter();
        var app = BuildApp(greeter);

        // Event Grid escalates an unhandled event (no matching handler) into a thrown exception so the
        // subscription's own retry/dead-lettering takes over - RaiseOnFailureStatus is on by default.
        // The queue transports instead just record the miss; this is Event Grid's safe-by-default shape.
        await Assert.ThrowsAsync<EventGridMessageProcessingException>(() => app.HandleEventGridEvents(
            MessageBuilder.Create("does.not-exist", new HelloWorldMessage { Name = "World" }).AsEventGridBenzeneMessage()));

        Assert.Empty(greeter.Greeted);
    }
}
