// See https://aka.ms/new-console-template for more information

using Confluent.Kafka;

namespace Benzene.Examples.Kafka.Test.Helpers;

public static class KafkaSenderHelper
{
    public static async Task Send()
    {
        var SomeStatus = "some-status";
        var SomeName = "some-name";

        ProducerConfig producerConfig = new()
        {
            BootstrapServers = "localhost:9092",
            SaslMechanism = SaslMechanism.Plain,
            SecurityProtocol = SecurityProtocol.Plaintext,
        };

        var sender = new KafkaSender(producerConfig);

        while (true)
        {
            var message = new { Status = SomeStatus, Name = SomeName };

            await sender.SendAsync("order_create", message);
            await Task.Delay(1);
        }
    }
}