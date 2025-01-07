using System.Threading.Tasks;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Aws.Kafka;

public class KafkaLambdaHandler : AwsLambdaMiddlewareRouter<KafkaEvent>
{
    private readonly IMiddlewareApplication<KafkaEvent> _application;

    public KafkaLambdaHandler(
        IMiddlewareApplication<KafkaEvent> application,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
        _application = application;
    }

    protected override bool CanHandle(KafkaEvent request)
    {
        return request?.EventSource == "aws:kafka";
    }

    protected override async Task HandleFunction(KafkaEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        // var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        // setCurrentTransport.SetTransport("kafka");
        await _application.HandleAsync(request, serviceResolverFactory);
    }
}
