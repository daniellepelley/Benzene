using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Kafka;
using Benzene.Azure.Function.Kafka.TestHelpers;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Test.Examples;
using Benzene.Testing;
using Xunit;

namespace Benzene.Test.Azure;

public class KafkaW3CTraceContextTest
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

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseKafka(kafka => kafka
                    .UseW3CTraceContext<KafkaContext>()
                    .Use((_, next) => next())))
            .Build();

        var request = MessageBuilder.Create("my-topic", new { Name = "value" })
            .WithHeaders(headers)
            .AsAzureKafkaEvent();

        await app.HandleKafkaEvents(request);

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
}
