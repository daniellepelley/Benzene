using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Aws.EventBridge;

public class S3LambdaHandler : AwsLambdaHandlerMiddleware<S3Event>
{
    private readonly IMiddlewareApplication<S3Event> _application;

    public S3LambdaHandler(
        IMiddlewareApplication<S3Event> application,   
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _application = application;
    }

    protected override bool CanHandle(S3Event request)
    {
        return request?.Records != null &&
               request.Records.Any() &&
               request.Records[0].EventSource == "aws:s3";
    }

    protected override async Task HandleFunction(S3Event request, AwsEventStreamContext context, IServiceResolver serviceResolver)
    {
        await _application.HandleAsync(request, serviceResolver);
    }
}
