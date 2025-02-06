using System.Reflection;
using Autofac;
using Benzene.Abstractions.Middleware;
using Benzene.Autofac;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.XRay;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Microsoft.Extensions.Configuration;
using Extensions = Benzene.Core.MessageHandlers.Extensions;

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
        services.UsingBenzene(x => Extensions.AddMessageHandlers(x
                .AddBenzene()
                .AddBenzeneMessage(), Assembly.GetExecutingAssembly())
        );
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app
            .UseXRayTracing(true)
            .UseBenzeneMessage(PipelineMother.BasicBenzeneMessagePipeline());
    }
}
