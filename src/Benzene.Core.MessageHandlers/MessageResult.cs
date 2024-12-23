using Benzene.Abstractions.Results;

namespace Benzene.Core.MessageHandlers;

public interface IDefaultStatuses
{
    public string ValidationError { get; }
    public string NotFound { get; }
}

public class MessageResult : IMessageResult
{
    public MessageResult(bool isSuccessful)
    {
        IsSuccessful = isSuccessful;
    }

    public bool IsSuccessful { get; }

}
