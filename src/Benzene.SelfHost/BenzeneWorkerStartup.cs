using Benzene.Abstractions.Hosting;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.SelfHost
{
    public abstract class BenzeneWorkerStartup :
        IStartUp<IServiceCollection, IConfiguration, IBenzeneWorkerBuilder>,
        IBenzeneWorker
    {
        private readonly IBenzeneWorker _worker;

        protected BenzeneWorkerStartup()
            : this(new ServiceCollection())
        { }

        protected BenzeneWorkerStartup(IServiceCollection services)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            var configuration = GetConfiguration();
            var app = new BenzeneWorkerBuilder(new MicrosoftBenzeneServiceContainer(services));

            // ReSharper disable once VirtualMemberCallInConstructor
            ConfigureServices(services, configuration);

            // ReSharper disable once VirtualMemberCallInConstructor
            Configure(app, configuration);
            
            var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
            _worker = app.Create(serviceResolverFactory);
        }

        public abstract IConfiguration GetConfiguration();

        public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        public abstract void Configure(IBenzeneWorkerBuilder app, IConfiguration configuration);
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _worker.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _worker.StopAsync(cancellationToken);
        }
    }
}
