using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class W3CTraceContextTest
{
    private static (List<Activity> Activities, ActivityListener Listener) ListenToBenzeneActivities()
    {
        var activities = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BenzeneDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return (activities, listener);
    }

    private static async Task<List<Activity>> RunPipeline(IDictionary<string, string> headers)
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage());
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();

        var builder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        builder.UseW3CTraceContext();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Headers = headers });
        await pipeline.HandleAsync(context, resolver);

        return activities;
    }

    [Fact]
    public async Task ValidTraceparent_BecomesTheActivitysParent()
    {
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        const string parentSpanId = "00f067aa0ba902b7";
        var activities = await RunPipeline(new Dictionary<string, string>
        {
            { "traceparent", $"00-{traceId}-{parentSpanId}-01" }
        });

        var activity = Assert.Single(activities, a => a.OperationName == "W3CTraceContext.Root");
        Assert.Equal(traceId, activity.TraceId.ToHexString());
        Assert.Equal(parentSpanId, activity.ParentSpanId.ToHexString());
    }

    [Fact]
    public async Task MissingTraceparent_StartsANewTrace_WithoutThrowing()
    {
        var activities = await RunPipeline(new Dictionary<string, string>());

        var activity = Assert.Single(activities, a => a.OperationName == "W3CTraceContext.Root");
        Assert.NotEqual(default, activity.TraceId);
    }

    [Fact]
    public async Task InvalidTraceparent_StartsANewTrace_WithoutThrowing()
    {
        var activities = await RunPipeline(new Dictionary<string, string> { { "traceparent", "not-a-valid-traceparent" } });

        var activity = Assert.Single(activities, a => a.OperationName == "W3CTraceContext.Root");
        Assert.NotEqual(default, activity.TraceId);
    }
}
