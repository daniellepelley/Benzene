using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.SelfHost;

public abstract class SelfHostedStartUp : IStartUp<IServiceCollection, IMiddlewarePipelineBuilder<SelfHostContext>>
{
    private readonly BenzeneHost _benzeneHost;

    protected SelfHostedStartUp()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        var configuration = GetConfiguration();
        var services = new ServiceCollection();
        var app = new MiddlewarePipelineBuilder<SelfHostContext>(new MicrosoftBenzeneServiceContainer(services));

        // ReSharper disable once VirtualMemberCallInConstructor
        ConfigureServices(services, configuration);

        // ReSharper disable once VirtualMemberCallInConstructor
        Configure(app, configuration);
        var pipeline = app.AsPipeline();

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        _benzeneHost = new BenzeneHost(new SelfHostMiddlewareApplication(pipeline, serviceResolverFactory));
    }

    public Task StartAsync(int port, CancellationToken cancellationToken = new())
    {
        return _benzeneHost.StartAsync(port, cancellationToken);
    }
    
    public abstract IConfiguration GetConfiguration();

    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    public abstract void Configure(IMiddlewarePipelineBuilder<SelfHostContext> app, IConfiguration configuration);
}
