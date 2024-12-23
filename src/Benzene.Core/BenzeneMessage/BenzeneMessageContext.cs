namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageContext 
{
    public BenzeneMessageContext(IBenzeneMessageRequest benzeneMessageRequest)
    {
        BenzeneMessageRequest = benzeneMessageRequest;
        BenzeneMessageResponse = new BenzeneMessageResponse();
    }

    public IBenzeneMessageRequest BenzeneMessageRequest { get; }
    public IBenzeneMessageResponse BenzeneMessageResponse { get; set; }
}
