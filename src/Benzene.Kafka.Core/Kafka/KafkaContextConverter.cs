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
    private readonly string _keyHeader;

    public KafkaContextConverter()
        :this(new JsonSerializer())
    { }

    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    /// <param name="keyHeader">
    /// The request header whose value becomes the Kafka message key (hash(key) → partition, so events
    /// sharing a key land on the same partition and are ordered there). <c>null</c> (the default) sends
    /// a keyless message (round-robin partitioning, no per-key ordering).
    /// </param>
    public KafkaContextConverter(ISerializer serializer, string keyHeader = null)
    {
        _serializer = serializer;
        _keyHeader = keyHeader;
    }

    public Task<KafkaSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Abstractions.Results.Void> contextIn)
    {
        var headers = new Headers();
        foreach (var header in contextIn.Request.Headers)
        {
            headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
        }

        string key = null;
        if (_keyHeader != null)
        {
            contextIn.Request.Headers.TryGetValue(_keyHeader, out key);
        }

        return Task.FromResult(new KafkaSendMessageContext(contextIn.Request.Topic,
            new Message<string, string>
            {
                Key = key,
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