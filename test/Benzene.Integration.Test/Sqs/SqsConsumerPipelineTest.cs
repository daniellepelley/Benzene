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

public class SqsConsumerPipelineTest : IClassFixture<SqsFixture>
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

        await sqsClient.CreateQueueAsync(new CreateQueueRequest("test-queue"));

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
}

public class DemoMessage
{
    public string Value { get; set; }
}
