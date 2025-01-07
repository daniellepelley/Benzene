using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Aws.Sns;

public class SnsClientMiddleware : IMiddleware<SnsSendMessageContext>
{
    private readonly IAmazonSimpleNotificationService _amazonSns;

    public SnsClientMiddleware(IAmazonSimpleNotificationService amazonSns)
    {
        _amazonSns = amazonSns;
    }
    
    public string Name => nameof(SnsClientMiddleware);

    public async Task HandleAsync(SnsSendMessageContext context, Func<Task> next)
    {
        context.Response = await _amazonSns.PublishAsync(context.Request);
    }
}