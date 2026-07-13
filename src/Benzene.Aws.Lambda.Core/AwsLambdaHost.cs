using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Hosts a platform-neutral <see cref="BenzeneStartUp"/> as an AWS Lambda entry point. Subclass with
/// your StartUp (<c>public class Function : AwsLambdaHost&lt;StartUp&gt; { }</c>) and point function-handler
/// at <c>YourAssembly::YourNamespace.Function::FunctionHandlerAsync</c>.
/// </summary>
public class AwsLambdaHost<TStartUp> : IAwsLambdaEntryPoint where TStartUp : BenzeneStartUp, new()
{
    private readonly AwsLambdaEntryPoint _entryPoint;

    /// <summary>
    /// Constructs <typeparamref name="TStartUp"/>, runs its configuration/service registration, and
    /// builds the middleware pipeline the Lambda entry point dispatches every invocation through.
    /// </summary>
    public AwsLambdaHost()
    {
        var startUp = new TStartUp();
        var configuration = startUp.GetConfiguration();
        var services = new ServiceCollection();
        services.AddLogging();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var eventPipeline = new MiddlewarePipelineBuilder<AwsEventStreamContext>(container);

        startUp.ConfigureServices(services, configuration);
        startUp.Configure(new AwsLambdaApplicationBuilder(eventPipeline, container), configuration);

        _entryPoint = new AwsLambdaEntryPoint(eventPipeline.Build(), new MicrosoftServiceResolverFactory(services));
    }

    /// <summary>
    /// The Lambda function handler entry point — point your function-handler setting at this method.
    /// </summary>
    /// <param name="stream">The raw invocation payload stream.</param>
    /// <param name="lambdaContext">The Lambda runtime's invocation context.</param>
    /// <returns>The raw response payload stream.</returns>
    public Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext lambdaContext) =>
        _entryPoint.FunctionHandlerAsync(stream, lambdaContext);

    /// <summary>
    /// Disposes the underlying entry point (and, transitively, its service resolver factory).
    /// </summary>
    public void Dispose() => _entryPoint.Dispose();
}
