using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Examples.App.Validators;
using Benzene.FluentValidation;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.Google;

/// <summary>
/// The one platform-neutral application definition this example uses to demonstrate hosting the
/// same Benzene app on both Cloud Run (<c>Program.cs</c>, via
/// <c>WebApplicationBuilder.UseBenzene&lt;Startup&gt;()</c>) and Cloud Functions Gen2
/// (<c>Function.cs</c>, via <c>GoogleCloudFunctionHost&lt;Startup&gt;</c>).
/// </summary>
public class Startup : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
            .AddHttpMessageHandlers());

        services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(asp => asp.UseMessageHandlers(router => router.UseFluentValidation()));
    }
}
