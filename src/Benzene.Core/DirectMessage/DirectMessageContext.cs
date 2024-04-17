using Benzene.Abstractions.Results;

namespace Benzene.Core.DirectMessage;

public class DirectMessageContext : IHasMessageResult
{
    private DirectMessageContext(IDirectMessageRequest directMessageRequest)
    {
        DirectMessageRequest = directMessageRequest;
        DirectMessageResponse = new DirectMessageResponse();
        MessageResult = Results.MessageResult.Empty();
    }

    public static DirectMessageContext CreateInstance(IDirectMessageRequest directMessageRequest)
    {
        return new DirectMessageContext(directMessageRequest);
    }

    public IDirectMessageRequest DirectMessageRequest { get; }
    public IDirectMessageResponse DirectMessageResponse { get; set; }
    public IMessageResult MessageResult { get; set; }
}
