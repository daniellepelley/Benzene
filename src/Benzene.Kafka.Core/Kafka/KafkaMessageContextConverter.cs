using System;
using System.Text;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Results;
using Confluent.Kafka;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaMessageContextConverter<TContext> : IContextConverter<TContext, KafkaSendMessageContext>
{
    // The routing-topic key. The outbound topic is set explicitly (below) from the topic getter, so a
    // "topic" carried in the inbound message's headers must NOT be copied onto the produced message:
    // a Benzene SQS/SNS source surfaces its routing topic as a header, and forwarding it here would
    // re-route the produced message to the consumed topic - an infinite loop if this service consumes
    // it. An outbound topic is always explicit, never inherited from the handled message's headers.
    private const string TopicHeaderKey = "topic";

    private readonly IMessageTopicGetter<TContext> _messageTopicGetter;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public KafkaMessageContextConverter(IMessageTopicGetter<TContext> messageTopicGetter, IMessageBodyGetter<TContext> messageBodyGetter, IMessageHeadersGetter<TContext> messageHeadersGetter, IMessageHandlerResultSetter<TContext> messageHandlerResultSetter, IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _messageTopicGetter = messageTopicGetter;
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _messageHeadersGetter = messageHeadersGetter;
        _messageBodyGetter = messageBodyGetter;
    }

    public Task<KafkaSendMessageContext> CreateRequestAsync(TContext contextIn)
    {
        var topic = _messageTopicGetter.GetTopic(contextIn)
            ?? throw new InvalidOperationException($"{typeof(IMessageTopicGetter<TContext>)} returned no topic for {typeof(TContext)}; a Kafka message cannot be produced without one.");

        // Forward headers onto the produced message, matching KafkaContextConverter. Previously
        // dropped here, which silently lost correlation-id / W3C trace-context on this produce path.
        // The routing-topic key is excluded (see TopicHeaderKey) so the inbound topic can't leak onto
        // the outbound message and re-route it to the topic being consumed.
        var headers = new Headers();
        var messageHeaders = _messageHeadersGetter.GetHeaders(contextIn);
        if (messageHeaders != null)
        {
            foreach (var header in messageHeaders)
            {
                if (string.Equals(header.Key, TopicHeaderKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Coalesce a null value to empty; GetBytes(null) would otherwise throw and fail the produce.
                headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value ?? string.Empty));
            }
        }

        return Task.FromResult(new KafkaSendMessageContext(
            topic.Id,
            new Message<string, string>
            {
                // A null body is a legitimate Kafka value (e.g. a tombstone record on a
                // compacted topic), so this is intentionally not defaulted to string.Empty.
                Value = _messageBodyGetter.GetBody(contextIn)!,
                Headers = headers
            }
        ));
    }

    public Task MapResponseAsync(TContext contextIn, KafkaSendMessageContext contextOut)
    {
        if (_benzeneResponseAdapter != null)
        {
            _benzeneResponseAdapter.SetStatusCode(contextIn, "Ok");
            return Task.CompletedTask;
        }

        return _messageHandlerResultSetter.SetResultAsync(contextIn,
            new MessageHandlerResult(new Topic(contextOut.Topic),
                MessageHandlerDefinition.Empty(), BenzeneResult.Set("Ok")));
    }
}