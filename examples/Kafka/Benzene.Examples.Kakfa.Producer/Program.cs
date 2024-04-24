// See https://aka.ms/new-console-template for more information

using Benzene.Examples.Kakfa.Producer;
using Confluent.Kafka;
using Newtonsoft.Json;

var SomeStatus = "some-status";
var SomeName = "some-name";

ProducerConfig producerConfig = new()
{
    BootstrapServers = "localhost:9092",
    SaslMechanism = SaslMechanism.Plain,
    SecurityProtocol = SecurityProtocol.Plaintext,
};

var sender = new KafkaSender(producerConfig);

for(var i = 0; i < 5; i++)
{       
    var message = new { Status = SomeStatus, Name = SomeName };

    await sender.SendAsync("order_create", message);
    await Task.Delay(1);
}




namespace Benzene.Examples.Kakfa.Producer
{
    public class KafkaSender : IDisposable
    {
        private readonly IProducer<Null, string>? _producer;

        public KafkaSender(ProducerConfig config)
        {
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public void Dispose()
        {
            _producer?.Flush();
            _producer?.Dispose();
        }

        public async Task SendAsync(string topic, object message)
        {
            await _producer?.ProduceAsync(topic, new Message<Null, string> { Value = JsonConvert.SerializeObject(message) });
        }
    }
}
