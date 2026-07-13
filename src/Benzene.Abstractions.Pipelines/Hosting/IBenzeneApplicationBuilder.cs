using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.Hosting;

/// <summary>Platform-neutral application builder passed to <see cref="BenzeneStartUp"/>.Configure.</summary>
public interface IBenzeneApplicationBuilder : IRegisterDependency
{
    /// <summary>The hosting platform identifier, e.g. "AwsLambda" or "Worker".</summary>
    string Platform { get; }

    /// <summary>Creates a middleware pipeline builder for the given context type.</summary>
    IMiddlewarePipelineBuilder<TContext> Create<TContext>();
}
