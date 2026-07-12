using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Exceptions;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Provides the default <see cref="IAwsLambdaEntryPoint"/> implementation, running a middleware
/// pipeline over an <see cref="AwsEventStreamContext"/> for each Lambda invocation.
/// </summary>
public class AwsLambdaEntryPoint : IAwsLambdaEntryPoint
{
    private readonly IMiddlewarePipeline<AwsEventStreamContext> _app;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaEntryPoint"/> class.
    /// </summary>
    /// <param name="app">The built middleware pipeline that processes each invocation.</param>
    /// <param name="serviceResolverFactory">The factory used to create a service resolver scope per invocation.</param>
    public AwsLambdaEntryPoint(IMiddlewarePipeline<AwsEventStreamContext> app, IServiceResolverFactory serviceResolverFactory)
    {
        _app = app;
        _serviceResolverFactory = serviceResolverFactory;
    }

    /// <summary>
    /// Handles a single AWS Lambda invocation by running the configured middleware pipeline.
    /// </summary>
    /// <param name="stream">The raw Lambda invocation payload stream.</param>
    /// <param name="lambdaContext">The AWS Lambda execution context for this invocation.</param>
    /// <returns>A task that resolves to the response stream written by the pipeline.</returns>
    /// <exception cref="BenzeneException">
    /// Thrown if no middleware in the pipeline handled the event and wrote a response — typically
    /// because there's no pipeline set up for this event type, or the event JSON is incomplete.
    /// </exception>
    public async Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext lambdaContext)
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

    /// <summary>
    /// Disposes the underlying service resolver factory.
    /// </summary>
    public void Dispose()
    {
        _serviceResolverFactory?.Dispose();
    }
}
