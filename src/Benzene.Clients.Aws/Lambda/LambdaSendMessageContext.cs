using Amazon.Lambda.Model;

namespace Benzene.Clients.Aws.Lambda;

public class LambdaSendMessageContext
{
    public LambdaSendMessageContext(InvokeRequest request)
    {
        Request = request;
    }
    public InvokeRequest Request { get; }
    public InvokeResponse Response { get; set; }
}