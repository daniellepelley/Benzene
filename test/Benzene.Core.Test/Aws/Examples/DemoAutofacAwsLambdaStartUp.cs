using System.Reflection;
using Autofac;
using Benzene.Abstractions.Middleware;
using Benzene.Autofac;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Test.Examples;
using Microsoft.Extensions.Configuration;

#pragma warning disable CS0618 // deliberately exercises the obsolete AwsLambdaStartUp model
namespace Benzene.Test.Aws.Examples;

public class DemoAutofacAwsLambdaStartUp : AwsLambdaStartUp<ContainerBuilder>
{
    public DemoAutofacAwsLambdaStartUp()
        :base(new AutofacDependencyInjectionAdapter())
    { }
    
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    public override void ConfigureServices(ContainerBuilder services, IConfiguration configuration)
    {
        ServiceResolverMother.ConfigureServiceCollection(services);
        services.UsingBenzene(x => x
                .AddMessageHandlers(Assembly.GetExecutingAssembly())
                .AddBenzene()
                .AddBenzeneMessage());
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app
            .UseBenzeneMessage(PipelineMother.BasicBenzeneMessagePipeline());
    }
}
