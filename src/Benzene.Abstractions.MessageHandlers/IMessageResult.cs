namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageResult
{
    bool IsSuccessful { get; }
}