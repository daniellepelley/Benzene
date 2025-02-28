using Benzene.Abstractions.Middleware;
using Confluent.Kafka;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaClientMiddleware : IMiddleware<KafkaSendMessageContext>
{
    private readonly IProducer<string, string> _producer;

    public KafkaClientMiddleware(IProducer<string, string> producer)
    {
        _producer = producer;
    }
    
    public string Name => nameof(KafkaClientMiddleware);

    public async Task HandleAsync(KafkaSendMessageContext context, Func<Task> next)
    {
        try
        {
            context.Response = await _producer.ProduceAsync(context.Topic, context.Message);
        }
        catch (Exception ex)
        {

        }
    }
}