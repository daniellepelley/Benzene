using Amazon.SimpleNotificationService.Model;

namespace Benzene.Clients.Aws.Sns;

public class SnsSendMessageContext
{
    public SnsSendMessageContext(PublishRequest request)
    {
        Request = request;
    }
    public PublishRequest Request { get; }
    public PublishResponse Response { get; set; }
}