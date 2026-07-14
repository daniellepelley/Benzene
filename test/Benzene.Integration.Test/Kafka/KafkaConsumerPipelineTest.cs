using System.Text;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Kafka;
using Benzene.Core.MessageHandlers;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.Testing;
using Confluent.Kafka;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Integration.Test.Kafka;

// Reuses the same Event Hubs emulator container as EventHubConsumerPipelineTest (it exposes a
// Kafka-compatible endpoint on port 9092 alongside its native AMQP port) - see
// DockerEmulatorCollection. Uses the "example" entity, a separate one from "eh1", so this test's
// produce/consume doesn't cross-contaminate with the AMQP-native Event Hub test.
[Collection(DockerEmulatorCollection.Name)]
public class KafkaConsumerPipelineTest
{
    private const string BootstrapServers = "localhost:9092";

    // Unlike Service Bus (a dedicated "topic" application property) or the Event Hub envelope (the
    // topic embedded in the JSON body), KafkaMessageTopicGetter routes on the literal Kafka topic
    // name itself - so this has to be Defaults.Topic, not just any isolation-purposed name, or the
    // message never matches ExampleMessageHandler's [Message(Defaults.Topic)] registration.
    private const string KafkaTopic = Defaults.Topic;

    // The emulator's Kafka-compatible endpoint wraps the same SAS-based auth as its native AMQP
    // endpoint (see EventHubConsumerPipelineTest's ConnectionString) via SASL PLAIN, not bare
    // PLAINTEXT with no auth - a plain PLAINTEXT connection gets accepted at the TCP level but then
    // immediately disconnected ("broker might require SASL authentication").
    private const string SaslUsername = "$ConnectionString";
    private const string SaslPassword =
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    [Fact]
    public async Task Send()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseKafka(kafka => kafka
                    .UseMessageHandlers()))
            .Build();

        // The emulator takes a few seconds to become ready after the containers start; retry the
        // initial produce rather than relying on a fixed sleep.
        await ProduceAsync();

        var record = await ConsumeAsync();

        var kafkaRecord = new KafkaRecord
        {
            Topic = record.Topic,
            Value = Encoding.UTF8.GetBytes(record.Message.Value)
        };

        await app.HandleKafkaEvents(kafkaRecord);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
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
            // Confluent.Kafka's default MessageTimeoutMs is 5 minutes, which would swallow this
            // method's whole 60-second retry loop on the first attempt while the emulator is still
            // starting up. Fail each attempt fast instead so the loop can actually retry.
            MessageTimeoutMs = 5000,
        };

        using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        // 180s, not 60s: on a cold CI runner this fixture's container may still be settling by
        // the time this test's own retry window starts, since collection-fixture construction for
        // every emulator in DockerEmulatorCollection happens up front, before any test runs.
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

    private static Task<ConsumeResult<Null, string>> ConsumeAsync()
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = SaslUsername,
            SaslPassword = SaslPassword,
            GroupId = $"benzene-integration-test-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();
        consumer.Subscribe(KafkaTopic);

        var result = consumer.Consume(TimeSpan.FromSeconds(60))
            ?? throw new TimeoutException("Timed out waiting to receive the message back from the Event Hubs emulator's Kafka endpoint.");

        consumer.Close();
        return Task.FromResult(result);
    }
}
