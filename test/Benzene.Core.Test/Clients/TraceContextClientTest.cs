using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Clients.TraceContext;
using Benzene.Results;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients;

public class TraceContextClientTest
{
    [Fact]
    public async Task StampsCurrentActivitysTraceparentOntoOutgoingHeaders()
    {
        using var activitySource = new ActivitySource("Test." + nameof(TraceContextClientTest));
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("outbound-call");

        IBenzeneClientRequest<string> captured = null;
        var mockInner = new Mock<IBenzeneMessageClient>();
        mockInner.Setup(x => x.SendMessageAsync<string, Void>(It.IsAny<IBenzeneClientRequest<string>>()))
            .Callback<IBenzeneClientRequest<string>>(request => captured = request)
            .ReturnsAsync(BenzeneResult.Accepted<Void>());

        var client = new TraceContextBenzeneMessageClient(mockInner.Object);
        await client.SendMessageAsync<string, Void>(new BenzeneClientRequest<string>("my-topic", "hello", new Dictionary<string, string>()));

        Assert.Equal(activity.Id, captured.Headers["traceparent"]);
    }

    [Fact]
    public async Task NoAmbientActivity_LeavesHeadersUnchanged()
    {
        IBenzeneClientRequest<string> captured = null;
        var mockInner = new Mock<IBenzeneMessageClient>();
        mockInner.Setup(x => x.SendMessageAsync<string, Void>(It.IsAny<IBenzeneClientRequest<string>>()))
            .Callback<IBenzeneClientRequest<string>>(request => captured = request)
            .ReturnsAsync(BenzeneResult.Accepted<Void>());

        var client = new TraceContextBenzeneMessageClient(mockInner.Object);
        await client.SendMessageAsync<string, Void>(new BenzeneClientRequest<string>("my-topic", "hello", new Dictionary<string, string>()));

        Assert.False(captured.Headers.ContainsKey("traceparent"));
    }
}
