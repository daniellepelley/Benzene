using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Example.Azure;
using Benzene.FluentValidation;
using Benzene.Http.Cors;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Example.Azure;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return DependenciesBuilder.GetConfiguration();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        DependenciesBuilder.Register(services, configuration);
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http
            .OnRequest("strip-api", x => x.HttpRequest.Path = x.HttpRequest.Path.Value.Replace("/api", ""))
            .UseCors(new CorsSettings
            {
                AllowedDomains = new []{ "https://editor-next.swagger.io" },
                AllowedHeaders = Array.Empty<string>()
            })
            .UseSpec()
            // Correlates topic/handler/invocationId onto the logging scope so they surface in
            // Application Insights' customDimensions - see docs/cookbooks/logging-application-insights.md.
            .UseBenzeneEnrichment()
            .UseMessageHandlers(router => router.UseFluentValidation()));
    }
}
