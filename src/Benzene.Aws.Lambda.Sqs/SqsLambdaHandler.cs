using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsLambdaHandler : AwsLambdaMiddlewareRouter<SQSEvent>
{
    private readonly IMiddlewareApplication<SQSEvent, SQSBatchResponse> _application;

    public SqsLambdaHandler(
        IMiddlewareApplication<SQSEvent, SQSBatchResponse> application,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _application = application;
    }

    protected override bool CanHandle(SQSEvent request)
    {
        return request?.Records != null &&
               request.Records.Any() &&
               request.Records[0].EventSource == "aws:sqs";
    }

    protected override async Task HandleFunction(SQSEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _application.HandleAsync(request, serviceResolverFactory);
        MapResponse(context, response);
    }
}
