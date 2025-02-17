using Amazon.Lambda.SQSEvents;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.Microsoft.Dependencies;
using Benzene.Testing;
using Benzene.Tools;
using Benzene.Tools.Aws;
using Benzene.Zipkin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using zipkin4net;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Benzene.Integration.Test;

public class SqsMessagePipelineTest : IClassFixture<ZipkinFixture>
{

    [Fact]
    public async Task Send()
    {
        var logger = new ZipkinLogger();

        TraceManager.SamplingRate = 1.0f;
        // Obtain the Zipkin endpoint in the Tracing Analysis console. Note that the endpoint does not contain /api/v2/spans.
        var sender = new HttpZipkinSender("http://localhost:9411", "application/json"); // It should implement IZipkinSender

        var tracer = new ZipkinTracer(sender, new JSONSpanSerializer());
        TraceManager.RegisterTracer(tracer);

        TraceManager.Start(logger);

        if (Trace.Current == null)
        {
            Trace.Current = Trace.Create();
            Trace.Current.Record(Annotations.ServiceName("Benzene"));
            Trace.Current.Record(Annotations.Rpc("topic"));
            Trace.Current.Record(Annotations.ConsumerStart());
        }

        var mockExampleService = new Mock<IExampleService>();

        bool? isSuccessful = null;

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ => NullLogger<MessageRouter<SqsMessageContext>>.Instance)
                .AddTransient<ILogger>(_ => NullLogger.Instance)
                .AddTransient(_ => mockExampleService.Object)
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddDiagnostics()
                    .AddSqs()
                    .AddZipkin())
                )
            .Configure(app => app
                .UseSqs(sqs => sqs
                    .OnResponse("Check Response", context =>
                    {
                        isSuccessful = context.IsSuccessful;
                    }).UseMessageHandlers()
            )
        ).BuildHost();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqs();

        SQSBatchResponse batchResponse = await host.SendSqsAsync(request);
        Assert.True(isSuccessful);
        Assert.Empty(batchResponse.BatchItemFailures);
        Trace.Current.Record(Annotations.ConsumerStop());

        TraceManager.Stop();
        await Task.Delay(2000);
    }

}
