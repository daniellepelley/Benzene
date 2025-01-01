using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.SQS;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Sqs;

namespace Benzene.Clients.Aws.Lambda;

public class AwsLambdaClientMiddleware : IMiddleware<LambdaSendMessageContext>
{
    private readonly IAmazonLambda _amazonLambda;

    public AwsLambdaClientMiddleware(IAmazonLambda amazonLambda)
    {
        _amazonLambda = amazonLambda;
    }
    
    public string Name => nameof(SqsClientMiddleware);

    public async Task HandleAsync(LambdaSendMessageContext context, Func<Task> next)
    {
        context.Response = await _amazonLambda.InvokeAsync(context.Request);
    }
}