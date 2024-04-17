using Benzene.Abstractions.MessageHandling;

namespace Benzene.Abstractions.Results;

public interface IMessageResult
{
    string Topic { get; }
    IMessageHandlerDefinition MessageHandlerDefinition { get; }
    string Status { get; }
    bool IsSuccessful { get; }
    object Payload { get; }
    string[] Errors { get; }
}