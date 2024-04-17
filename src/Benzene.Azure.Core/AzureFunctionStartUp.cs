﻿using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Azure.Core;

public abstract class AzureFunctionStartUp : IWebJobsStartup, IStartUp<IServiceCollection, AzureAppBuilder>
{
    public abstract IConfiguration GetConfiguration();

    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    public abstract void Configure(AzureAppBuilder app, IConfiguration configuration);

    public void Configure(IWebJobsBuilder builder)
    {
        var configuration = GetConfiguration();
        ConfigureServices(builder.Services, configuration);
        var app = new AzureAppBuilder(new MicrosoftBenzeneServiceContainer(builder.Services));
        Configure(app, configuration);

        builder.Services.AddScoped<IAzureApp>(serviceProvider => app.Create(new MicrosoftServiceResolverFactory(serviceProvider)));
    }
}
