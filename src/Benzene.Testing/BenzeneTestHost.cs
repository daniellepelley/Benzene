using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Testing;

/// <summary>
/// Entry point for building an in-memory test host from a platform-neutral <see cref="BenzeneStartUp"/>.
/// </summary>
public static class BenzeneTestHost
{
    /// <summary>
    /// Starts building a test host for <typeparamref name="TStartUp"/>. Call a platform-specific
    /// <c>Build*</c> extension (e.g. <c>BuildAwsLambdaHost</c>, <c>BuildAzureFunctionApp</c>) to finish.
    /// </summary>
    /// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
    public static BenzeneTestHostBuilder<TStartUp> Create<TStartUp>() where TStartUp : BenzeneStartUp, new()
    {
        return new BenzeneTestHostBuilder<TStartUp>();
    }
}

/// <summary>
/// Builds an in-memory test host for a <see cref="BenzeneStartUp"/>, with optional configuration and
/// service overrides applied on top of the StartUp's own registrations.
/// </summary>
/// <typeparam name="TStartUp">The <see cref="BenzeneStartUp"/> to run.</typeparam>
public class BenzeneTestHostBuilder<TStartUp> where TStartUp : BenzeneStartUp, new()
{
    private readonly List<Action<IServiceCollection>> _serviceActions = new();
    private readonly Dictionary<string, string?> _configOverrides = new();

    /// <summary>
    /// Registers an action that runs after the StartUp's own <c>ConfigureServices</c>, typically used to
    /// replace a real dependency with a fake or mock.
    /// </summary>
    /// <param name="action">The action that registers or overrides services.</param>
    /// <returns>This instance, for method chaining.</returns>
    public BenzeneTestHostBuilder<TStartUp> WithServices(Action<IServiceCollection> action)
    {
        _serviceActions.Add(action);
        return this;
    }

    /// <summary>
    /// Overrides a single configuration value on top of the StartUp's own <c>GetConfiguration</c> result.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>This instance, for method chaining.</returns>
    public BenzeneTestHostBuilder<TStartUp> WithConfiguration(string key, string value)
    {
        _configOverrides[key] = value;
        return this;
    }

    /// <summary>
    /// Overrides configuration values on top of the StartUp's own <c>GetConfiguration</c> result.
    /// </summary>
    /// <param name="values">The configuration values to override.</param>
    /// <returns>This instance, for method chaining.</returns>
    public BenzeneTestHostBuilder<TStartUp> WithConfiguration(IDictionary<string, string?> values)
    {
        foreach (var value in values)
        {
            _configOverrides[value.Key] = value.Value;
        }

        return this;
    }

    /// <summary>
    /// Runs the StartUp's <c>GetConfiguration</c>/<c>ConfigureServices</c> plus any overrides, then hands
    /// the result to <paramref name="factory"/> to build a platform-specific host. Platform packages
    /// (e.g. <c>Benzene.Aws.Lambda.Core</c>, <c>Benzene.Azure.Function.Core</c>) call this from their own
    /// <c>Build*</c> extension methods to construct their concrete <c>IBenzeneApplicationBuilder</c> and
    /// call <c>startUp.Configure(...)</c> against it.
    /// </summary>
    /// <typeparam name="THost">The platform-specific host/app type to return.</typeparam>
    /// <param name="factory">Builds the platform-specific host from the StartUp, configured services, and configuration.</param>
    /// <returns>The platform-specific host built by <paramref name="factory"/>.</returns>
    public THost Build<THost>(Func<TStartUp, IServiceCollection, IConfiguration, THost> factory)
    {
        var startUp = new TStartUp();
        var configuration = new ConfigurationBuilder()
            .AddConfiguration(startUp.GetConfiguration())
            .AddInMemoryCollection(_configOverrides)
            .Build();

        var services = new ServiceCollection();
        startUp.ConfigureServices(services, configuration);

        foreach (var action in _serviceActions)
        {
            action(services);
        }

        return factory(startUp, services, configuration);
    }
}
