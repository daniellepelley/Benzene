using Benzene.Aws.Sqs.Consumer;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.Testing;

namespace Benzene.Aws.Sqs.TestHelpers;

/// <summary>
/// Provides the standalone SQS polling consumer bridge for <see cref="BenzeneTestHostBuilder{TStartUp}"/>.
/// </summary>
public static class BenzeneTestHostExtensions
{
    /// <summary>
    /// Builds a <see cref="SqsConsumerBenzeneTestHost"/> from the StartUp, configured services, and any
    /// overrides registered on <paramref name="builder"/> — the same message pipeline <c>UseSqs</c>
    /// builds for a real worker, with a seam for test overrides but no queue connection or AWS
    /// credentials. Push a message through it with <see cref="SqsConsumerBenzeneTestHost.HandleAsync"/>.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built SQS consumer test host.</returns>
    public static SqsConsumerBenzeneTestHost BuildSqsConsumerHost<TStartUp>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var container = new MicrosoftBenzeneServiceContainer(services);
            startUp.Configure(new WorkerApplicationBuilder(container), configuration);

            var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
            using var scope = serviceResolverFactory.CreateScope();
            var application = scope.GetService<SqsConsumerApplication>();

            return new SqsConsumerBenzeneTestHost(application, serviceResolverFactory);
        });
    }
}
