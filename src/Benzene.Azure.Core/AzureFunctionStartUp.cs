using Benzene.Abstractions.Hosting;
using Benzene.Microsoft.Dependencies;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Azure.Core;

/// <summary>
/// Provides the base class for configuring an Azure Functions app using the WebJobs SDK's startup
/// extensibility point (<see cref="IWebJobsStartup"/>), together with Microsoft's built-in dependency
/// injection container.
/// </summary>
/// <remarks>
/// Inherit from this class and reference it via <c>[assembly: WebJobsStartup(typeof(YourStartUp))]</c> so
/// the Azure Functions host discovers and invokes it. On <see cref="Configure(IWebJobsBuilder)"/>, this
/// calls <see cref="GetConfiguration"/>, <see cref="ConfigureServices"/>, and <see cref="Configure(AzureFunctionAppBuilder, IConfiguration)"/>
/// (in that order) to build the middleware pipeline, then registers a scoped <see cref="IAzureFunctionApp"/>
/// that individual function triggers resolve and dispatch to.
/// </remarks>
public abstract class AzureFunctionStartUp : IWebJobsStartup, IStartUp<IServiceCollection, IConfiguration, AzureFunctionAppBuilder>
{
    /// <summary>
    /// Builds the configuration for this Azure Functions app.
    /// </summary>
    /// <returns>The configuration to use for service registration and pipeline setup.</returns>
    public abstract IConfiguration GetConfiguration();

    /// <summary>
    /// Registers services with the dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to register services with.</param>
    /// <param name="configuration">The configuration built by <see cref="GetConfiguration"/>.</param>
    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Configures the entry point applications for this Azure Functions app.
    /// </summary>
    /// <param name="app">The Azure Function app builder to configure, typically by adding entry point applications for each trigger type.</param>
    /// <param name="configuration">The configuration built by <see cref="GetConfiguration"/>.</param>
    public abstract void Configure(AzureFunctionAppBuilder app, IConfiguration configuration);

    /// <summary>
    /// Called by the Azure Functions WebJobs host to configure the app. Builds the configuration, service
    /// container, and entry point applications, then registers a scoped <see cref="IAzureFunctionApp"/>.
    /// </summary>
    /// <param name="builder">The WebJobs builder provided by the host.</param>
    public void Configure(IWebJobsBuilder builder)
    {
        var configuration = GetConfiguration();
        ConfigureServices(builder.Services, configuration);
        var app = new AzureFunctionAppBuilder(new MicrosoftBenzeneServiceContainer(builder.Services));
        Configure(app, configuration);

        builder.Services.AddScoped<IAzureFunctionApp>(serviceProvider => app.Create(new MicrosoftServiceResolverFactory(serviceProvider)));
    }
}
