using Benzene.Abstractions.DI;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.GoogleCloud.Functions.Core;

/// <summary>
/// Shared bootstrap steps every Google Cloud Functions trigger-type package needs before it can run
/// a platform-neutral <see cref="BenzeneStartUp"/>'s <c>Configure</c> - constructing the StartUp,
/// reading its configuration, and preparing the <see cref="IServiceCollection"/>/
/// <see cref="IBenzeneServiceContainer"/> pair <c>ConfigureServices</c>/<c>Configure</c> are run
/// against. Factored out so each trigger-type package (<c>Benzene.GoogleCloud.Functions.Http</c>
/// today, a future <c>Benzene.GoogleCloud.Functions.PubSub</c>) doesn't duplicate it, mirroring why
/// <c>Benzene.Aws.Lambda.Core</c> exists as a shared foundation for its own event-source packages.
/// </summary>
public static class GoogleCloudStartUpRunner
{
    /// <summary>
    /// Constructs a <typeparamref name="TStartUp"/> and prepares everything needed to run its
    /// <c>ConfigureServices</c>/<c>Configure</c> lifecycle.
    /// </summary>
    /// <typeparam name="TStartUp">The platform-neutral application definition to bootstrap.</typeparam>
    /// <returns>
    /// The constructed <typeparamref name="TStartUp"/> instance, its resolved configuration, the
    /// <see cref="IServiceCollection"/> to register services into, and the
    /// <see cref="IBenzeneServiceContainer"/> wrapping it that <c>Configure</c> expects.
    /// </returns>
    public static (TStartUp StartUp, IConfiguration Configuration, IServiceCollection Services, IBenzeneServiceContainer Container)
        Bootstrap<TStartUp>() where TStartUp : BenzeneStartUp, new()
    {
        var startUp = new TStartUp();
        var configuration = startUp.GetConfiguration();
        var services = new ServiceCollection();
        services.AddLogging();
        var container = new MicrosoftBenzeneServiceContainer(services);

        return (startUp, configuration, services, container);
    }
}
