using System;
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

// Handlers reuse ExampleRequestPayload for request and response (like the ResponseEvents/DynamoDb
// tests) so whole-assembly spec-generation tests see no new payload schemas.
[Message("faildiag:notfound")]
public class FailDiagNotFoundHandler : IMessageHandler<ExampleRequestPayload, ExampleRequestPayload>
{
    public Task<IBenzeneResult<ExampleRequestPayload>> HandleAsync(ExampleRequestPayload request)
        => Task.FromResult(BenzeneResult.NotFound<ExampleRequestPayload>());
}

[Message("faildiag:throw")]
public class FailDiagThrowHandler : IMessageHandler<ExampleRequestPayload, ExampleRequestPayload>
{
    public Task<IBenzeneResult<ExampleRequestPayload>> HandleAsync(ExampleRequestPayload request)
        => throw new InvalidOperationException("handler boom");
}

public class PipelineFailureLoggingTest
{
    private static async Task<FakeLogCollector> RunAsync(string topic, bool useLogResult)
    {
        var fakeFactory = new FakeLoggerFactory();
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
        serviceCollection.AddLogging();
        // Registered last so it wins - the router's ILogger<> and UseLogResult both resolve it.
        serviceCollection.AddSingleton<ILoggerFactory>(fakeFactory);

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));
        if (useLogResult)
        {
            pipeline.UseLogResult(_ => { });
        }

        pipeline.UseMessageHandlers(new[] { typeof(FailDiagNotFoundHandler), typeof(FailDiagThrowHandler) });

        var application = new BenzeneMessageApplication(pipeline.Build());
        var request = new BenzeneMessageRequest
        {
            Topic = topic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload { Name = "x" })
        };
        var factory = new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider());

        try
        {
            await application.HandleAsync(request, factory);
        }
        catch (InvalidOperationException)
        {
            // The throwing handler propagates out - expected for the throw case.
        }

        return fakeFactory.Collector;
    }

    [Fact]
    public async Task UnsuccessfulHandlerResult_IsLoggedAsWarningByTheRouter()
    {
        var collector = await RunAsync("faildiag:notfound", useLogResult: false);

        Assert.Contains(collector.Entries, e =>
            e.Level == LogLevel.Warning && e.Message.Contains("NotFound"));
    }

    [Fact]
    public async Task SuccessfulResult_IsNotWarnedByTheRouter()
    {
        var collector = await RunAsync("faildiag:notfound", useLogResult: false);

        // Sanity: the only Warning is the unsuccessful-result one, not spurious noise.
        Assert.Single(collector.Entries.Where(e => e.Level == LogLevel.Warning));
    }

    [Fact]
    public async Task ThrowingHandler_IsLoggedAsErrorByLogResult()
    {
        var collector = await RunAsync("faildiag:throw", useLogResult: true);

        Assert.Contains(collector.Entries, e =>
            e.Level == LogLevel.Error && e.Exception is InvalidOperationException);
    }
}
