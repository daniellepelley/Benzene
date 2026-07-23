using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.Testing;

namespace Benzene.Azure.EventHub.TestHelpers;

/// <summary>
/// Provides the Event Hub worker bridge for <see cref="BenzeneTestHostBuilder{TStartUp}"/>.
/// </summary>
public static class BenzeneTestHostExtensions
{
    /// <summary>
    /// Builds an <see cref="EventHubWorkerBenzeneTestHost"/> from the StartUp, configured services, and
    /// any overrides registered on <paramref name="builder"/> — the same message pipeline
    /// <c>UseEventHub</c> builds for a real worker, with a seam for test overrides but no hub
    /// connection. Push an event through it with <see cref="EventHubWorkerBenzeneTestHost.HandleAsync"/>.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built Event Hub worker test host.</returns>
    public static EventHubWorkerBenzeneTestHost BuildEventHubWorkerHost<TStartUp>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var container = new MicrosoftBenzeneServiceContainer(services);
            startUp.Configure(new WorkerApplicationBuilder(container), configuration);

            var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
            using var scope = serviceResolverFactory.CreateScope();
            var application = scope.GetService<EventHubConsumerApplication>();

            return new EventHubWorkerBenzeneTestHost(application, serviceResolverFactory);
        });
    }
}
