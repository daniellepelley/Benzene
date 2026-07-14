using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Testing;

namespace Benzene.Aws.Lambda.Core.TestHelpers;

/// <summary>
/// Provides the AWS Lambda bridge for <see cref="BenzeneTestHostBuilder{TStartUp}"/>.
/// </summary>
public static class BenzeneTestHostExtensions
{
    /// <summary>
    /// Builds an <see cref="IAwsLambdaEntryPoint"/> from the StartUp, configured services, and any
    /// overrides registered on <paramref name="builder"/> — the same construction
    /// <see cref="AwsLambdaHost{TStartUp}"/> performs for a real deployment, with a seam for test
    /// overrides. Wrap the result in <c>AwsLambdaBenzeneTestHost</c> (in <c>Benzene.Tools</c>) to send
    /// events into it.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built entry point.</returns>
    public static IAwsLambdaEntryPoint BuildAwsLambdaHost<TStartUp>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var container = new MicrosoftBenzeneServiceContainer(services);
            var eventPipeline = new MiddlewarePipelineBuilder<AwsEventStreamContext>(container);

            startUp.Configure(new AwsLambdaApplicationBuilder(eventPipeline, container), configuration);

            return new AwsLambdaEntryPoint(eventPipeline.Build(), new MicrosoftServiceResolverFactory(services));
        });
    }
}
