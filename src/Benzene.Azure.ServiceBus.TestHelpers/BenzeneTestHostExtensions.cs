using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.Testing;

namespace Benzene.Azure.ServiceBus.TestHelpers;

/// <summary>
/// Provides the Service Bus worker bridge for <see cref="BenzeneTestHostBuilder{TStartUp}"/>.
/// </summary>
public static class BenzeneTestHostExtensions
{
    /// <summary>
    /// Builds a <see cref="ServiceBusWorkerBenzeneTestHost"/> from the StartUp, configured services, and
    /// any overrides registered on <paramref name="builder"/> — the same message pipeline
    /// <c>UseServiceBus</c> builds for a real worker, with a seam for test overrides but no broker
    /// connection. Push a message through it with <see cref="ServiceBusWorkerBenzeneTestHost.HandleAsync"/>.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built Service Bus worker test host.</returns>
    public static ServiceBusWorkerBenzeneTestHost BuildServiceBusWorkerHost<TStartUp>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var container = new MicrosoftBenzeneServiceContainer(services);
            startUp.Configure(new WorkerApplicationBuilder(container), configuration);

            var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
            using var scope = serviceResolverFactory.CreateScope();
            var application = scope.GetService<ServiceBusConsumerApplication>();

            return new ServiceBusWorkerBenzeneTestHost(application, serviceResolverFactory);
        });
    }
}
