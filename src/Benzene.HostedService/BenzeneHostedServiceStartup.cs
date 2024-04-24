using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benzene.HostedService
{
    public abstract class BenzeneHostedServiceStartup :
        IStartUp<IServiceCollection, IHostedServiceAppBuilder>,
        IHostedService
    {
        private readonly IBenzeneConsumer _consumer;

        protected BenzeneHostedServiceStartup()
            : this(new ServiceCollection())
        { }

        protected BenzeneHostedServiceStartup(IServiceCollection services)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            var configuration = GetConfiguration();
            var app = new HostedServiceAppBuilder(new MicrosoftBenzeneServiceContainer(services));

            // ReSharper disable once VirtualMemberCallInConstructor
            ConfigureServices(services, configuration);

            // ReSharper disable once VirtualMemberCallInConstructor
            Configure(app, configuration);
            
            var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
            _consumer = app.Create(serviceResolverFactory);
        }

        public abstract IConfiguration GetConfiguration();

        public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        public abstract void Configure(IHostedServiceAppBuilder app, IConfiguration configuration);
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _consumer.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _consumer.StopAsync(cancellationToken);
        }
    }
}
