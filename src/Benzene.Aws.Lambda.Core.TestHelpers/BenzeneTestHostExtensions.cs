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
    /// overrides. Wrap the result in <c>AwsLambdaBenzeneTestHost</c> (in this project) to send events
    /// into it.
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

    /// <summary>
    /// Builds a ready-to-use <see cref="AwsLambdaBenzeneTestHost"/> from the StartUp and any
    /// <c>WithServices</c>/<c>WithConfiguration</c> overrides — the fluent, single-call form, so you can
    /// go straight from <c>BenzeneTestHost.Create&lt;StartUp&gt;()</c> to a host you can
    /// <c>SendEventAsync</c> on without a separate <c>new AwsLambdaBenzeneTestHost(...)</c> wrap. This
    /// mirrors the worker test-host builders (<c>BuildRabbitMqWorkerHost</c>/<c>BuildServiceBusWorkerHost</c>/
    /// <c>BuildEventHubWorkerHost</c>), which already return their test host directly. Use
    /// <see cref="BuildAwsLambdaHost{TStartUp}"/> instead when you only need the raw
    /// <see cref="IAwsLambdaEntryPoint"/> (e.g. to hand to a base class that wraps it itself).
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built, ready-to-use test host.</returns>
    public static AwsLambdaBenzeneTestHost BuildAwsLambdaTestHost<TStartUp>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return new AwsLambdaBenzeneTestHost(builder.BuildAwsLambdaHost());
    }
}
