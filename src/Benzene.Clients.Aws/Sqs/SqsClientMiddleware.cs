using System;
using System.Threading.Tasks;
using Amazon.SQS;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Aws.Sqs;

public class SqsClientMiddleware : IMiddleware<SqsSendMessageContext>
{
    private readonly IAmazonSQS _amazonSqs;

    public SqsClientMiddleware(IAmazonSQS amazonSqs)
    {
        _amazonSqs = amazonSqs;
    }
    
    public string Name => nameof(SqsClientMiddleware);

    public async Task HandleAsync(SqsSendMessageContext context, Func<Task> next)
    {
        context.Response = await _amazonSqs.SendMessageAsync(context.Request);
    }
}