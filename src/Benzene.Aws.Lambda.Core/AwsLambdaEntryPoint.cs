using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Exceptions;

namespace Benzene.Aws.Lambda.Core;

public class AwsLambdaEntryPoint : IAwsLambdaEntryPoint
{
    private readonly IMiddlewarePipeline<AwsEventStreamContext> _app;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    public AwsLambdaEntryPoint(IMiddlewarePipeline<AwsEventStreamContext> app, IServiceResolverFactory serviceResolverFactory)
    {
        _app = app;
        _serviceResolverFactory = serviceResolverFactory;
    }

    public async Task<Stream> FunctionHandler(Stream stream, ILambdaContext lambdaContext)
    {
        using var scope = _serviceResolverFactory.CreateScope();

        var context = new AwsEventStreamContext(stream, lambdaContext);
        await _app.HandleAsync(context, scope);

        if (context.Response != null)
        {
            return context.Response;
        }

        throw new BenzeneException("The event type has not been recognized. It is possible that there isn't a pipeline set up that can handle this event type, or the JSON for the event is not complete, for instance the EventSource field is missing");
    }

    public void Dispose()
    {
        _serviceResolverFactory?.Dispose();
    }
}
