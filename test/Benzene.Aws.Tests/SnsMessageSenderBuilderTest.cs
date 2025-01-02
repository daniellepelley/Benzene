using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Tests.Fixtures;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.MessageSender;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Xunit;

namespace Benzene.Aws.Tests;

[Collection("Sequential")]
public class SnsMessageSenderBuilderTest : IClassFixture<SqsFixture>
{
    private string? _topicArn;
    private const string ServiceUrl = "http://localhost:4566";
    private const string AccessKey = "123";
    private const string SecretKey = "xyz";

    private async Task SetUp()
    {
        var amazonSnsClient = CreateAmazonSnsClient();

        var result = await amazonSnsClient.CreateTopicAsync(new CreateTopicRequest("some-topic"));
        _topicArn = result.TopicArn;
    }

    private static IAmazonSimpleNotificationService CreateAmazonSnsClient()
    {
        return new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials(AccessKey, SecretKey), new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = ServiceUrl,
        });
    }

    [Fact]
    public async Task Sns_Send()
    {
        await SetUp();
        var amazonSnsClient = CreateAmazonSnsClient();

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddSingleton(amazonSnsClient);
        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseSns(_topicArn, builder2 => builder2.UseSnsClient())));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope()
            .GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload { Name = "some-name" });

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Sns_Send2()
    {
        await SetUp();
        var amazonSnsClient = CreateAmazonSnsClient();

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddSingleton(amazonSnsClient);
        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseSns(_topicArn, builder2 => builder2.UseSnsClient())));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload { Name = "some-name" });

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Sns_Send3()
    {
        await SetUp();
        var amazonSnsClient = CreateAmazonSnsClient();

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddSingleton(amazonSnsClient);
        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseSns(_topicArn)));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload { Name = "some-name" });

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }
}