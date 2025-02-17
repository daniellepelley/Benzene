using System;
using System.Collections.Generic;
using Autofac;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Autofac;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Microsoft.Extensions.Configuration;

namespace Benzene.Examples.Aws.Tests.Helpers;

public class AutofacTestLambdaStartUp<TStartUp> where TStartUp : IStartUp<ContainerBuilder, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>, IAwsLambdaEntryPoint
{
    private readonly List<Action<ContainerBuilder>> _actions = new();
    private Dictionary<string, string> _dictionary;

    public AutofacTestLambdaStartUp<TStartUp> WithConfiguration(Dictionary<string, string> dictionary)
    {
        _dictionary = dictionary;
        return this;
    }

    public AutofacTestLambdaStartUp<TStartUp> WithServices(Action<ContainerBuilder> action)
    {
        _actions.Add(action);
        return this;
    }

    public IAwsLambdaEntryPoint Build()
    {
        var startup = Activator.CreateInstance<TStartUp>();

        var configuration = startup.GetConfiguration();
        var configurationBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(_dictionary);

        var services = new ContainerBuilder();
        var app = new AwsEventStreamPipelineBuilder(new AutofacBenzeneServiceContainer(services));
        
        startup.ConfigureServices(services, configurationBuilder.Build());
        foreach (var action in _actions)
        {
            action(services);
        }
        
        startup.Configure(app, configuration);

        var serviceResolverFactory = new AutofacServiceResolverFactory(services);
        return new AwsLambdaEntryPoint(app.Build(), serviceResolverFactory);
    }
}