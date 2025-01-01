namespace Benzene.Abstractions.MessageHandlers.ToDelete;

public interface IHasMessageResult
{
    IMessageResult MessageResult { get; set; }
}