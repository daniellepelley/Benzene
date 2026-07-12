using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Azure.Core;

/// <summary>
/// Provides an inline, fluent alternative to <see cref="AzureFunctionStartUp"/> for building an
/// <see cref="IAzureFunctionApp"/> without a dedicated startup class — useful for tests and small
/// standalone hosts.
/// </summary>
public class InlineAzureFunctionStartUp
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IAzureFunctionAppBuilder> _appAction = _ => { };

    /// <summary>
    /// Registers an action that configures services with the dependency injection container.
    /// </summary>
    /// <param name="action">The action that performs service registration.</param>
    /// <returns>This instance, for method chaining.</returns>
    public InlineAzureFunctionStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }

    /// <summary>
    /// Registers an action that configures the entry point applications for this app.
    /// </summary>
    /// <param name="action">The action that configures the Azure Function app builder.</param>
    /// <returns>This instance, for method chaining.</returns>
    public InlineAzureFunctionStartUp Configure(Action<IAzureFunctionAppBuilder> action)
    {
        _appAction = action;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="IAzureFunctionApp"/> using a fresh service collection and the previously
    /// registered configuration actions.
    /// </summary>
    /// <returns>The built Azure Function app.</returns>
    public IAzureFunctionApp Build()
    {
        var services = new ServiceCollection();
        var app = new AzureFunctionAppBuilder(new MicrosoftBenzeneServiceContainer(services));

        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return app.Create(serviceResolverFactory);
    }
}
