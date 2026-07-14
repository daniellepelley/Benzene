using System.Text;
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
        var headers = new Headers();
        foreach (var header in contextIn.Request.Headers)
        {
            headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
        }

        return Task.FromResult(new KafkaSendMessageContext(contextIn.Request.Topic,
            new Message<string, string>
            {
                Value = _serializer.Serialize(contextIn.Request.Message),
                Headers = headers
            }));
    }

    public Task MapResponseAsync(IBenzeneClientContext<T, Abstractions.Results.Void> contextIn, KafkaSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response?.Status == PersistenceStatus.Persisted
            ? BenzeneResult.Accepted<Abstractions.Results.Void>()
            : BenzeneResult.ServiceUnavailable<Abstractions.Results.Void>("Kafka message was not persisted");
        return Task.CompletedTask;
    }
}