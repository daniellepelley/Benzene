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
// EventHubEmulatorCollection. Uses the "kafka1" entity, a separate one from "eh1", so this test's
// produce/consume doesn't cross-contaminate with the AMQP-native Event Hub test.
[Collection(EventHubEmulatorCollection.Name)]
public class KafkaConsumerPipelineTest
{
    private const string BootstrapServers = "localhost:9092";
    private const string KafkaTopic = "kafka1";

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
            SecurityProtocol = SecurityProtocol.Plaintext,
        };

        using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        var deadline = DateTime.UtcNow.AddSeconds(60);
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
            SecurityProtocol = SecurityProtocol.Plaintext,
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
