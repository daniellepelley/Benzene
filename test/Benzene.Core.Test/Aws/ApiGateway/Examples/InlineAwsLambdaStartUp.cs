using System;
using Autofac;
using Benzene.Abstractions.Middleware;
using Benzene.Autofac;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Test.Aws.ApiGateway.Examples;

public class AutofacInlineAwsLambdaStartUp : IAwsEntryPointBuilder
{
    private Action<ContainerBuilder> _servicesAction = _ => { };
    private Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> _appAction = _ => { };

    public AutofacInlineAwsLambdaStartUp ConfigureServices(Action<ContainerBuilder> action)
    {
        _servicesAction = action;
        return this;
    }
    
    public AutofacInlineAwsLambdaStartUp Configure(Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> action)
    {
        _appAction = action;
        return this;
    }

    public IAwsLambdaEntryPoint Build()
    {
        var containerBuilder = new ContainerBuilder();
        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new AutofacBenzeneServiceContainer(containerBuilder));
        
        _appAction(app);
        _servicesAction(containerBuilder);

        var serviceResolverFactory = new AutofacServiceResolverFactory(containerBuilder);
        return new AwsLambdaEntryPoint(app.Build(), serviceResolverFactory);
    }
}
