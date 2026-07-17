using System.Diagnostics;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Clients.TraceContext;
using Xunit;

namespace Benzene.Test.Clients;

public class W3CTraceContextMiddlewareTest
{
    [Fact]
    public async Task HandleAsync_ActiveActivity_StampsTraceparentOntoContextHeaders()
    {
        using var activitySource = new ActivitySource("Test." + nameof(W3CTraceContextMiddlewareTest));
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("outbound-call");

        var context = new OutboundContext("my-topic", "hello");
        var middleware = new W3CTraceContextMiddleware();
        var nextCalled = false;

        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.Equal(activity.Id, context.Headers["traceparent"]);
    }

    [Fact]
    public async Task HandleAsync_NoAmbientActivity_LeavesHeadersUnchanged()
    {
        var context = new OutboundContext("my-topic", "hello");
        var middleware = new W3CTraceContextMiddleware();

        await middleware.HandleAsync(context, () => Task.CompletedTask);

        Assert.False(context.Headers.ContainsKey("traceparent"));
    }
}
