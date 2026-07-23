using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Test.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Diagnostics;

// Reuses ExampleRequestPayload for request and response (like the sibling failure-logging test) so
// whole-assembly spec-generation tests see no new payload schemas.
[Message("loglevel:ok")]
public class LogLevelOkHandler : IMessageHandler<ExampleRequestPayload, ExampleRequestPayload>
{
    public Task<IBenzeneResult<ExampleRequestPayload>> HandleAsync(ExampleRequestPayload request)
        => Task.FromResult(BenzeneResult.Ok(request));
}

public class LogResultLevelGatingTest
{
    private static async Task<(FakeLogCollector Collector, int OnResponseCalls)> RunAsync(LogLevel minLevel)
    {
        var fakeFactory = new FakeLoggerFactory(minLevel);
        var onResponseCalls = 0;

        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
        serviceCollection.AddLogging();
        // Registered last so it wins - UseLogResult resolves it.
        serviceCollection.AddSingleton<ILoggerFactory>(fakeFactory);

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));
        // An OnResponse extractor stands in for the real cost (it could read/serialize the result).
        pipeline.UseLogResult(log => log.OnResponse((_, _) =>
        {
            onResponseCalls++;
            return new Dictionary<string, string>();
        }));
        pipeline.UseMessageHandlers(new[] { typeof(LogLevelOkHandler) });

        var application = new BenzeneMessageApplication(pipeline.Build());
        var request = new BenzeneMessageRequest
        {
            Topic = "loglevel:ok",
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload { Name = "x" })
        };
        var factory = new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider());

        await application.HandleAsync(request, factory);

        return (fakeFactory.Collector, onResponseCalls);
    }

    [Fact]
    public async Task InformationEnabled_LogsBenzeneResult_AndRunsResponseExtractor()
    {
        var (collector, onResponseCalls) = await RunAsync(LogLevel.Trace);

        Assert.Contains(collector.Entries, e => e.Level == LogLevel.Information && e.Message == "BenzeneResult");
        Assert.Equal(1, onResponseCalls);
    }

    [Fact]
    public async Task InformationDisabled_SkipsBenzeneResultLog_AndDoesNotRunResponseExtractor()
    {
        var (collector, onResponseCalls) = await RunAsync(LogLevel.Warning);

        Assert.DoesNotContain(collector.Entries, e => e.Level == LogLevel.Information && e.Message == "BenzeneResult");
        // The point of the gate: the OnResponse extractor (potentially deserializing the result) never ran.
        Assert.Equal(0, onResponseCalls);
    }
}
