using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Azure.Core;

/// <summary>
/// Builds an <see cref="IAzureFunctionApp"/> by collecting entry point applications and providing
/// middleware pipeline creation and service registration.
/// </summary>
public interface IAzureFunctionAppBuilder : IRegisterDependency
{
    /// <summary>
    /// Registers a factory for an entry point application to be included in the built
    /// <see cref="IAzureFunctionApp"/>.
    /// </summary>
    /// <param name="func">A factory that creates the entry point application given the current invocation's service resolver factory.</param>
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func);

    /// <summary>
    /// Creates a new middleware pipeline builder for a given context type, sharing this builder's
    /// underlying service container.
    /// </summary>
    /// <typeparam name="TNewContext">The context type the pipeline operates on.</typeparam>
    /// <returns>The created pipeline builder.</returns>
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();

    /// <summary>
    /// Builds the <see cref="IAzureFunctionApp"/> from the registered entry point application factories.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to construct each entry point application.</param>
    /// <returns>The built Azure Function app.</returns>
    IAzureFunctionApp Create(IServiceResolverFactory serviceResolverFactory);
}
