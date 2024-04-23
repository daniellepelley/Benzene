using System;
using Autofac;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Autofac;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Test.Aws.ApiGateway.Examples;

public class AutofacInlineAwsLambdaStartUp : IEntryPointBuilder
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
        var app = new AwsEventStreamPipelineBuilder(new AutofacBenzeneServiceContainer(containerBuilder));
        
        _appAction(app);
        _servicesAction(containerBuilder);

        var serviceResolverFactory = new AutofacServiceResolverFactory(containerBuilder);
        return new AwsLambdaEntryPoint(app.Build(), serviceResolverFactory);
    }
}
