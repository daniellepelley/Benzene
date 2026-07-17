using Benzene.Core.MessageHandlers;
using Benzene.HostedService;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.Kafka.Core;
using Benzene.SelfHost;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Benzene.Integration.Test.Kafka;

// Reuses the same Event Hubs emulator container as KafkaConsumerPipelineTest/EventHubConsumerPipelineTest
// (see DockerEmulatorCollection) - its Kafka-compatible endpoint on port 9092. Unlike
// KafkaConsumerPipelineTest (which exercises Benzene.Azure.Function.Kafka's trigger-driven
// KafkaApplication against a single hand-built KafkaRecord), this test exercises
// Benzene.Kafka.Core.BenzeneKafkaWorker's own consume loop end to end: a real IConsumer polling a
// real broker, dispatched through BoundedConcurrentDispatcher, into the same message-handler
// pipeline. Both tests produce to Defaults.Topic/ExampleMessageHandler with identical message
// content, so whichever test's consumer happens to read a message the other produced is harmless -
// the assertion (IExampleService.Register(Defaults.Name)) holds either way.
[Collection(DockerEmulatorCollection.Name)]
public class BenzeneKafkaWorkerLiveTest
{
    private const string BootstrapServers = "localhost:9092";
    private const string KafkaTopic = Defaults.Topic;

    private const string SaslUsername = "$ConnectionString";
    private const string SaslPassword =
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    [Fact]
    public async Task StartAsync_ConsumesRealKafkaMessage_DispatchesThroughPipeline()
    {
        var mockExampleService = new Mock<IExampleService>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mockExampleService
            .Setup(x => x.Register(It.IsAny<string>()))
            .Callback(() => received.TrySetResult());

        var kafkaConfig = new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig
            {
                BootstrapServers = BootstrapServers,
                SecurityProtocol = SecurityProtocol.SaslPlaintext,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = SaslUsername,
                SaslPassword = SaslPassword,
                GroupId = $"benzene-integration-test-{Guid.NewGuid()}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
            },
            Topics = new[] { KafkaTopic },
            ConcurrentRequests = 1,
        };

        var inlineSelfHostedStartUp = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object))
            .Configure(app => app.UseKafka<Ignore, string>(kafkaConfig, kafka => kafka.UseMessageHandlers()));

        var host = new HostBuilder()
            .ConfigureServices(services =>
                services.AddHostedService(_ => inlineSelfHostedStartUp.BuildHostedService()))
            .Build();

        await host.StartAsync();
        try
        {
            await ProduceAsync();

            var completedTask = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(60)));
            Assert.Same(received.Task, completedTask);
        }
        finally
        {
            await host.StopAsync();
        }

        mockExampleService.Verify(x => x.Register(Defaults.Name), Times.AtLeastOnce);
    }

    private static async Task ProduceAsync()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = BootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = SaslUsername,
            SaslPassword = SaslPassword,
            MessageTimeoutMs = 5000,
        };

        using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        var deadline = DateTime.UtcNow.AddSeconds(180);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await producer.ProduceAsync(KafkaTopic, new Message<Null, string> { Value = Defaults.Message });
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("Timed out waiting for the Event Hubs emulator's Kafka endpoint to become ready.", lastException);
    }
}
