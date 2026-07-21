using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Example.Asp.Minimal;

/// <summary>
/// Platform-neutral application definition. This is the exact same <see cref="BenzeneStartUp"/>
/// shape the AWS Lambda and Kafka examples use - only the transport wired inside
/// <see cref="Configure"/> differs. Hosted by ASP.NET Core via
/// <c>WebApplicationBuilder.UseBenzene&lt;StartUp&gt;()</c> + <c>app.UseBenzene()</c> in Program.cs.
/// </summary>
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
        => app.UseHttp(http => http
            .UseMessageHandlers());
}
