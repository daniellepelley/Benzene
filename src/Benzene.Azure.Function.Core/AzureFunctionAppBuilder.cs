using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.Core;

/// <summary>
/// Default implementation of <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public class AzureFunctionAppBuilder : IAzureFunctionAppBuilder
{
    private readonly List<Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>> _apps = new();
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFunctionAppBuilder"/> class.
    /// </summary>
    /// <param name="benzeneServiceContainer">The service container used for registrations and pipeline building.</param>
    public AzureFunctionAppBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    /// <summary>
    /// Registers a factory for an entry point application to be included in the built
    /// <see cref="IAzureFunctionApp"/>.
    /// </summary>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func)
    {
        _apps.Add(func);
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

    /// <summary>
    /// Registers services with the underlying service container.
    /// </summary>
    /// <param name="action">The action that performs the registration.</param>
    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(_benzeneServiceContainer);
    }

    /// <summary>
    /// Creates a new middleware pipeline builder for a given context type, sharing this builder's
    /// underlying service container.
    /// </summary>
    /// <typeparam name="TNewContext">The context type the pipeline operates on.</typeparam>
    /// <returns>The created pipeline builder.</returns>
    public IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>()
    {
        return new MiddlewarePipelineBuilder<TNewContext>(_benzeneServiceContainer);
    }
}
