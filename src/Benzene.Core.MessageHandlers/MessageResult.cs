using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;

namespace Benzene.Core.MessageHandlers;

public interface IDefaultStatuses
{
    public string ValidationError { get; }
    public string NotFound { get; }
}

public class MessageResult : IMessageResult
{
    public static MessageResult Failure(string status)
    {
        return new MessageResult(Constants.Missing, MessageHandlers.MessageHandlerDefinition.Empty(),
            status, false, null, []);
    }

    public static MessageResult Failure(string status, string error)
    {
        return new MessageResult(Constants.Missing, MessageHandlers.MessageHandlerDefinition.Empty(),
            status, false, null, [error]);
    }

    public static MessageResult Failure(ITopic topic, string status, string error)
    {
        return new MessageResult(Constants.Missing, MessageHandlers.MessageHandlerDefinition.Empty(),
            status, false, null, [error]);
    }

    public MessageResult(ITopic? topic, IMessageHandlerDefinition? messageHandlerDefinition, string status, bool isSuccessful, object? payload, string[] errors)
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
        return new MessageResult(Constants.Missing, MessageHandlers.MessageHandlerDefinition.Empty(),
            string.Empty, false, null, Array.Empty<string>());
    }

    public ITopic? Topic { get; }
    public IMessageHandlerDefinition? MessageHandlerDefinition { get; }
    public string Status { get; }
    public bool IsSuccessful { get; }
    public object? Payload { get; }
    public string[] Errors { get; }
}
