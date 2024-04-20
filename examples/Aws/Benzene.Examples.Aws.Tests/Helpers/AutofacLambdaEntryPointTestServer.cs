using System;
using System.Collections.Generic;
using Autofac;
using Benzene.Autofac;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.MiddlewareBuilder;
using Microsoft.Extensions.Configuration;

namespace Benzene.Examples.Aws.Tests.Helpers;

public class AutofacTestLambdaStartUp<TStartUp> where TStartUp : IAwsStartUp<ContainerBuilder, AwsEventStreamContext>
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

    public ILambdaEntryPoint Build()
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
        return new LambdaEntryPoint(app.AsPipeline(), serviceResolverFactory);
    }
}