using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Autofac;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Autofac;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Microsoft.Extensions.Configuration;

namespace Benzene.Examples.Aws;

public abstract class AutofacAwsStartUp : IStartUp<ContainerBuilder, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>, IAwsLambdaEntryPoint
{
    private readonly AwsLambdaEntryPoint _awsLambdaEntryPoint;

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
        var pipeline = app.Build();
        
        var serviceResolverFactory = new AutofacServiceResolverFactory(configurationBuilder);
        _awsLambdaEntryPoint = new AwsLambdaEntryPoint(pipeline, serviceResolverFactory);
    }

    public abstract IConfiguration GetConfiguration();

    public abstract void ConfigureServices(ContainerBuilder services, IConfiguration configuration);

    public abstract void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> middlewarePipeline, IConfiguration configuration);

    public Task<Stream> FunctionHandler(Stream stream, ILambdaContext lambdaContext)
    {
        return _awsLambdaEntryPoint.FunctionHandler(stream, lambdaContext);
    }

    public void Dispose()
    {
        _awsLambdaEntryPoint.Dispose();
    }

}