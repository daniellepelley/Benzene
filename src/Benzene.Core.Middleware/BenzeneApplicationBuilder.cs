using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Base <see cref="IBenzeneApplicationBuilder"/> implementation every hosting platform subclasses
/// (e.g. AWS Lambda's <c>AwsLambdaApplicationBuilder</c>, the generic host's
/// <c>WorkerApplicationBuilder</c>) - platform subclasses supply their own <see cref="Platform"/>
/// constant and any platform-specific pipeline/state alongside the base's <see cref="Register"/>/
/// <see cref="Create{TContext}"/> plumbing.
/// </summary>
public class BenzeneApplicationBuilder : IBenzeneApplicationBuilder
{
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneApplicationBuilder"/> class.
    /// </summary>
    /// <param name="platform">The hosting platform identifier, e.g. "AwsLambda" or "Worker".</param>
    /// <param name="benzeneServiceContainer">The service container backing this builder.</param>
    public BenzeneApplicationBuilder(string platform, IBenzeneServiceContainer benzeneServiceContainer)
    {
        Platform = platform;
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    /// <inheritdoc />
    public string Platform { get; }

    /// <inheritdoc />
    public void Register(Action<IBenzeneServiceContainer> action) => action(_benzeneServiceContainer);

    /// <inheritdoc />
    public IMiddlewarePipelineBuilder<TContext> Create<TContext>() =>
        new MiddlewarePipelineBuilder<TContext>(_benzeneServiceContainer);
}
