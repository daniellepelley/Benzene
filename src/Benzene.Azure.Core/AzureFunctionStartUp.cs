﻿using Benzene.Abstractions.Hosting;
using Benzene.Microsoft.Dependencies;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Azure.Core;

public abstract class AzureFunctionStartUp : IWebJobsStartup, IStartUp<IServiceCollection, IConfiguration, AzureFunctionAppBuilder>
{
    public abstract IConfiguration GetConfiguration();

    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    public abstract void Configure(AzureFunctionAppBuilder app, IConfiguration configuration);

    public void Configure(IWebJobsBuilder builder)
    {
        var configuration = GetConfiguration();
        ConfigureServices(builder.Services, configuration);
        var app = new AzureFunctionAppBuilder(new MicrosoftBenzeneServiceContainer(builder.Services));
        Configure(app, configuration);

        builder.Services.AddScoped<IAzureFunctionApp>(serviceProvider => app.Create(new MicrosoftServiceResolverFactory(serviceProvider)));
    }
}
