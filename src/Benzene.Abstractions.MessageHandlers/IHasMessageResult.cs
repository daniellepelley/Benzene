namespace Benzene.Abstractions.MessageHandlers;

public interface IHasMessageResult
{
    IMessageResult MessageResult { get; set; }
}