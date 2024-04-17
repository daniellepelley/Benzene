using System;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Results;

public class MessageResult : IMessageResult
{
    public MessageResult(string topic, IMessageHandlerDefinition messageHandlerDefinition, string status, bool isSuccessful, object payload, string[] errors)
    {
        MessageHandlerDefinition = messageHandlerDefinition;
        Topic = topic;
        Status = status;
        IsSuccessful = isSuccessful;
        Payload = payload;
        Errors = errors;
    }

    public static MessageResult Empty()
    {
        return new MessageResult(Constants.Missing, MessageHandling.MessageHandlerDefinition.Empty(),
            string.Empty, false, null, Array.Empty<string>());
    }

    public string Topic { get; }
    public IMessageHandlerDefinition MessageHandlerDefinition { get; }
    public string Status { get; }
    public bool IsSuccessful { get; }
    public object Payload { get; }
    public string[] Errors { get; }
}
