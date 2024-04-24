using Confluent.Kafka;
using Newtonsoft.Json;

namespace Benzene.Examples.Kafka.Test.Helpers;

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
        await _producer?.ProduceAsync(topic.Replace(":", "_"), new Message<Null, string> { Value = JsonConvert.SerializeObject(message) });
    }
}