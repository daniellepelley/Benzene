using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Aws.Sns;

public class SnsLambdaHandler : AwsLambdaHandlerMiddleware<SNSEvent>
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

    protected override async Task HandleFunction(SNSEvent request, AwsEventStreamContext context, IServiceResolver serviceResolver)
    {
        await _application.HandleAsync(request, serviceResolver);
    }
}
