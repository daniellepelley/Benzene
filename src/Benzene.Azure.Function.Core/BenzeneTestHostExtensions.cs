using Benzene.Microsoft.Dependencies;
using Benzene.Testing;

namespace Benzene.Azure.Function.Core;

/// <summary>
/// Provides the Azure Functions bridge for <see cref="BenzeneTestHostBuilder{TStartUp}"/>.
/// </summary>
public static class BenzeneTestHostExtensions
{
    /// <summary>
    /// Builds an <see cref="IAzureFunctionApp"/> from the StartUp, configured services, and any
    /// overrides registered on <paramref name="builder"/> — the same construction
    /// <see cref="HostBuilderExtensions.UseBenzene{TStartUp}"/> performs for a real deployment, with a
    /// seam for test overrides. Dispatch into it directly with <c>HandleHttpRequest</c>,
    /// <c>HandleEventHub</c>, <c>HandleKafkaEvents</c>, etc.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    /// <param name="builder">The test host builder, with any <c>WithServices</c>/<c>WithConfiguration</c> overrides already applied.</param>
    /// <returns>The built Azure Function app.</returns>
    public static IAzureFunctionApp BuildAzureFunctionApp<TStartUp>(this BenzeneTestHostBuilder<TStartUp> builder)
        where TStartUp : BenzeneStartUp, new()
    {
        return builder.Build((startUp, services, configuration) =>
        {
            var container = new MicrosoftBenzeneServiceContainer(services);
            var appBuilder = new AzureFunctionAppBuilder(container);

            startUp.Configure(appBuilder, configuration);

            return appBuilder.Create(new MicrosoftServiceResolverFactory(services));
        });
    }
}
