using Benzene.Abstractions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

/// <summary>
/// Platform-neutral application definition. Derive once; run on any Benzene host
/// (AwsLambdaHost&lt;TStartUp&gt;, IHostBuilder.UseBenzene&lt;TStartUp&gt;()).
/// </summary>
public abstract class BenzeneStartUp : IStartUp<IServiceCollection, IConfiguration, IBenzeneApplicationBuilder>
{
    public abstract IConfiguration GetConfiguration();
    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    public abstract void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration);
}
