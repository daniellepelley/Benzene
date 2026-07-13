using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class BenzeneApplicationBuilder : IBenzeneApplicationBuilder
{
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public BenzeneApplicationBuilder(string platform, IBenzeneServiceContainer benzeneServiceContainer)
    {
        Platform = platform;
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    public string Platform { get; }

    public void Register(Action<IBenzeneServiceContainer> action) => action(_benzeneServiceContainer);

    public IMiddlewarePipelineBuilder<TContext> Create<TContext>() =>
        new MiddlewarePipelineBuilder<TContext>(_benzeneServiceContainer);
}
