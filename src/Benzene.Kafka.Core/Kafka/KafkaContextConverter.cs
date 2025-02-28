using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Results;
using Confluent.Kafka;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Abstractions.Results.Void>, KafkaSendMessageContext>
{
    private readonly ISerializer _serializer;

    public KafkaContextConverter()
        :this(new JsonSerializer())
    { }
    
    public KafkaContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public Task<KafkaSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Abstractions.Results.Void> contextIn)
    {
        return Task.FromResult(new KafkaSendMessageContext(contextIn.Request.Topic,
            new Message<string, string>
            {
                Value = _serializer.Serialize(contextIn.Request.Message)
            }));
    }

    public Task MapResponseAsync(IBenzeneClientContext<T, Abstractions.Results.Void> contextIn, KafkaSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Ok<Abstractions.Results.Void>();
        return Task.CompletedTask;
    }
}