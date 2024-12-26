namespace Benzene.Abstractions.MessageHandlers.ToDelete;

public interface IMessageResult
{
    bool IsSuccessful { get; }
}