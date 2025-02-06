using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Aws.Lambda.Core;

public class InlineAwsLambdaStartUp : IAwsEntryPointBuilder
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> _appAction = _ => { };

    public InlineAwsLambdaStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }
    
    public InlineAwsLambdaStartUp Configure(Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> action)
    {
        _appAction = action;
        return this;
    }

    public IAwsLambdaEntryPoint Build()
    {
        var services = new ServiceCollection();
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services));
        
        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        return new AwsLambdaEntryPoint(app.Build(), serviceResolverFactory);
    }
}
