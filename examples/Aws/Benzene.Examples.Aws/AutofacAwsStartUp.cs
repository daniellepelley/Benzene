using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Autofac;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Autofac;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.MiddlewareBuilder;
using Microsoft.Extensions.Configuration;

namespace Benzene.Examples.Aws;

public abstract class AutofacAwsStartUp : IAwsStartUp<ContainerBuilder, AwsEventStreamContext>, ILambdaEntryPoint
{
    private readonly LambdaEntryPoint _lambdaEntryPoint;

    protected AutofacAwsStartUp()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        var configuration = GetConfiguration();
        var configurationBuilder = new ContainerBuilder();
        var app = new AwsEventStreamPipelineBuilder(new AutofacBenzeneServiceContainer(configurationBuilder));

        // ReSharper disable once VirtualMemberCallInConstructor
        ConfigureServices(configurationBuilder, configuration);
        
        // ReSharper disable once VirtualMemberCallInConstructor
        Configure(app, configuration);
        var pipeline = app.AsPipeline();
        
        var serviceResolverFactory = new AutofacServiceResolverFactory(configurationBuilder);
        _lambdaEntryPoint = new LambdaEntryPoint(pipeline, serviceResolverFactory);
    }

    public abstract IConfiguration GetConfiguration();

    public abstract void ConfigureServices(ContainerBuilder services, IConfiguration configuration);

    public abstract void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> middlewarePipeline, IConfiguration configuration);

    public Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext lambdaContext)
    {
        return _lambdaEntryPoint.FunctionHandlerAsync(stream, lambdaContext);
    }
}