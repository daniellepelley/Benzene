using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Sns;

public class SnsLambdaHandler : AwsLambdaMiddlewareRouter<SNSEvent>
{
    private readonly IMiddlewareApplication<SNSEvent> _application;

    public SnsLambdaHandler(
        IMiddlewareApplication<SNSEvent> application,   
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _application = application;
    }

    protected override bool CanHandle(SNSEvent request)
    {
        return request?.Records != null &&
               request.Records.Any() &&
               request.Records[0].EventSource == "aws:sns";
    }

    protected override async Task HandleFunction(SNSEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        await _application.HandleAsync(request, serviceResolverFactory);
    }
}
