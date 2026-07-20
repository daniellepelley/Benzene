using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.Core;

/// <summary>
/// Default implementation of <see cref="IAzureFunctionAppBuilder"/>. Also implements the
/// platform-neutral <see cref="Benzene.Abstractions.Hosting.IBenzeneApplicationBuilder"/> (via
/// <see cref="BenzeneApplicationBuilder"/>) so a <see cref="Benzene.Microsoft.Dependencies.BenzeneStartUp"/>
/// can be configured identically whether hosted on Azure Functions or any other Benzene host.
/// </summary>
public class AzureFunctionAppBuilder : BenzeneApplicationBuilder, IAzureFunctionAppBuilder
{
    /// <summary>The platform identifier reported by <see cref="BenzeneApplicationBuilder.Platform"/>.</summary>
    public const string PlatformName = "AzureFunctions";

    private readonly List<(string? Key, Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> Factory)> _apps = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFunctionAppBuilder"/> class.
    /// </summary>
    /// <param name="benzeneServiceContainer">The service container used for registrations and pipeline building.</param>
    public AzureFunctionAppBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        : base(PlatformName, benzeneServiceContainer)
    {
    }

    /// <summary>
    /// Registers a factory for an entry point application to be included in the built
    /// <see cref="IAzureFunctionApp"/>.
    /// </summary>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func)
    {
        Add(null, func);
    }

    /// <summary>
    /// Registers a factory under a discriminator <paramref name="key"/>, so multiple entry points of
    /// the same request type (e.g. two <c>[QueueTrigger]</c> functions) can coexist and be dispatched
    /// to by name. A <c>null</c> key registers a type-only entry point (the default).
    /// </summary>
    /// <param name="key">The discriminator key (typically the function/queue/topic name), or <c>null</c>.</param>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    public void Add(string? key, Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func)
    {
        _apps.Add((key, func));
    }

    /// <summary>
    /// Builds the <see cref="IAzureFunctionApp"/> from the registered entry point application factories.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to construct each entry point application.</param>
    /// <returns>The built Azure Function app.</returns>
    public IAzureFunctionApp Create(IServiceResolverFactory serviceResolverFactory)
    {
        return new AzureFunctionApp(_apps.ToArray(), serviceResolverFactory);
    }
}
