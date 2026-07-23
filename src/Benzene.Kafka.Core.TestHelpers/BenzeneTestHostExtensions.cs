using Benzene.Kafka.Core.KafkaMessage;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.Testing;

namespace Benzene.Kafka.Core.TestHelpers;

/// <summary>
/// Provides the Kafka worker bridge for <see cref="BenzeneTestHostBuilder{TStartUp}"/>.
/// </summary>
public static class BenzeneTestHostExtensions
{
    /// <summary>
    /// Builds a <see cref="KafkaBenzeneTestHost{TKey, TValue}"/> from the StartUp, configured services,
    /// and any overrides registered on <paramref name="builder"/> — the same message pipeline
    /// <c>UseKafka</c> builds for a real worker, with a seam for test overrides but no broker
    /// connection. The <typeparamref name="TKey"/>/<typeparamref name="TValue"/> must match the
    /// <c>UseKafka&lt;TKey, TValue&gt;</c> the StartUp configured. Push a record through it with
    /// <see cref="KafkaBenzeneTestHost{TKey, TValue}.HandleAsync"/>.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <typeparam name="TKey">The Kafka record key type.</typeparam>
    /// <typeparam name="TValue">The Kafka record value type.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built Kafka worker test host.</returns>
    public static KafkaBenzeneTestHost<TKey, TValue> BuildKafkaWorkerHost<TStartUp, TKey, TValue>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var container = new MicrosoftBenzeneServiceContainer(services);
            startUp.Configure(new WorkerApplicationBuilder(container), configuration);

            var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
            using var scope = serviceResolverFactory.CreateScope();
            var application = scope.GetService<KafkaApplication<TKey, TValue>>();

            return new KafkaBenzeneTestHost<TKey, TValue>(application, serviceResolverFactory);
        });
    }
}
