using System.Reflection;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Core.DirectMessage;
using Benzene.Aws.XRay;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Test.Aws.Examples;

public class DemoAwsLambdaStartUp : AwsLambdaStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ServiceResolverMother.ConfigureServiceCollection(services);
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddBenzeneMessage()
            .AddMessageHandlers2(Assembly.GetExecutingAssembly())
        );
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app
            .UseXRayTracing(true)
            .UseBenzeneMessage(PipelineMother.BasicBenzeneMessagePipeline());
    }
}
