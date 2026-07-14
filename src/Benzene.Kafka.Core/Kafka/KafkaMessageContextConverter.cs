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
        return Task.FromResult(new KafkaSendMessageContext(
            _messageTopicGetter.GetTopic(contextIn).Id,
            new Message<string, string>
            {
                Value = _messageBodyGetter.GetBody(contextIn)
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