using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.HostedService;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Benzene.Integration.Test.Sqs;

[Collection(DockerEmulatorCollection.Name)]
public class SqsConsumerPipelineTest
{
    [Fact]
    public async Task Send()
    {
        const string queueUrl = "http://localhost:4566/000000000000/test-queue";
        var list = new List<string>();

        var sqsClient = new AmazonSQSClient(
            new AnonymousAWSCredentials(),
            new AmazonSQSConfig
            {
                ServiceURL = "http://localhost:4566",
            });

        // LocalStack takes a few seconds to become ready after the container starts; retry the
        // initial call rather than relying on a fixed sleep (same pattern as the other
        // emulator-backed fixtures in this project).
        await CreateQueueWithRetryAsync(sqsClient);

        var sqsClientFactory = new SqsClientFactory(sqsClient);

        var inlineSelfHostedStartUp = new InlineSelfHostedStartUp()
            .ConfigureServices(x => x
                .UsingBenzene(b => b
                    .AddBenzene()
                ))
            .Configure(x => x.UseSqs(new SqsConsumerConfig
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10
            },
            sqsClientFactory,
            x => x
            .OnRequest(r =>
            {
                list.Add(r.Message.Body);
            })
        ));

        var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.UsingBenzene(b => b
                    .AddBenzene()
                    .AddSqsMessageClient(queueUrl, s => s.UseSqsClient(sqsClient))
                );
                services.AddHostedService(x => inlineSelfHostedStartUp.BuildHostedService());
            })
            .Build();

        var sqsBenzeneMessageClient = host.Services.GetService<SqsBenzeneMessageClient>()! as IBenzeneMessageClient;

        await sqsBenzeneMessageClient.SendMessageAsync(Defaults.Topic, new DemoMessage { Value = "foo" });

        host.StartAsync();

        await Task.Delay(1000);
        await host.StopAsync();

        Assert.Equal(1, list.Count);
        Assert.Contains("foo", list[0]);
    }

    private static async Task CreateQueueWithRetryAsync(AmazonSQSClient sqsClient)
    {
        // 180s, not 60s: on a cold CI runner this fixture's container may still be settling by
        // the time this test's own retry window starts, since collection-fixture construction for
        // every emulator in DockerEmulatorCollection happens up front, before any test runs.
        var deadline = DateTime.UtcNow.AddSeconds(180);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await sqsClient.CreateQueueAsync(new CreateQueueRequest("test-queue"));
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("Timed out waiting for the LocalStack SQS emulator to become ready.", lastException);
    }
}

public class DemoMessage
{
    public string Value { get; set; }
}
