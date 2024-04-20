using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Benzene.AspNet.Core;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.Google;

public class HttpFunction : IHttpFunction
{
    private readonly AspNetApplication _app;

    public HttpFunction()
        :this(DependenciesBuilder.CreateServiceResolverFactory(DependenciesBuilder.GetConfiguration()))
    { }

    public HttpFunction(IServiceCollection services)
    {
        var pipeline = new MiddlewarePipelineBuilder<AspNetContext>(new MicrosoftBenzeneServiceContainer(services))
            .UseTimer("asp-net")
            .UseProcessResponse()
            .UseMessageRouter(router => router.UseFluentValidation());

        _app = new AspNetApplication(pipeline.AsPipeline(), new MicrosoftServiceResolverFactory(services));
    }


    public async Task HandleAsync(HttpContext context)
    {
        await _app.HandleAsync(context);
    }
}