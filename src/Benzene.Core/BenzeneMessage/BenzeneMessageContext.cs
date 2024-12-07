using Benzene.Abstractions.Results;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageContext : IHasMessageResult
{
    private BenzeneMessageContext(IBenzeneMessageRequest benzeneMessageRequest)
    {
        BenzeneMessageRequest = benzeneMessageRequest;
        BenzeneMessageResponse = new BenzeneMessageResponse();
        MessageResult = null;//Results.MessageResult.Empty();
    }

    public static BenzeneMessageContext CreateInstance(IBenzeneMessageRequest benzeneMessageRequest)
    {
        return new BenzeneMessageContext(benzeneMessageRequest);
    }

    public IBenzeneMessageRequest BenzeneMessageRequest { get; }
    public IBenzeneMessageResponse BenzeneMessageResponse { get; set; }
    public IMessageResult MessageResult { get; set; }
}
