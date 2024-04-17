using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Aws.Core;

public abstract class AwsLambdaStartUp : IStartUp<IServiceCollection, IMiddlewarePipelineBuilder<AwsEventStreamContext>>, IAwsLambdaEntryPoint
{
    private readonly AwsLambdaEntryPoint _awsLambdaEntryPoint;

    protected AwsLambdaStartUp()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        var configuration = GetConfiguration();
        var services = new ServiceCollection();
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services));
        
        // ReSharper disable once VirtualMemberCallInConstructor
        ConfigureServices(services, configuration);
        
        // ReSharper disable once VirtualMemberCallInConstructor
        Configure(app, configuration);
        var pipeline = app.AsPipeline();
        
        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        _awsLambdaEntryPoint = new AwsLambdaEntryPoint(pipeline, serviceResolverFactory);
    }

    public abstract IConfiguration GetConfiguration();

    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    public abstract void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration);

    public Task<Stream> FunctionHandler(Stream stream, ILambdaContext lambdaContext)
    {
        return _awsLambdaEntryPoint.FunctionHandler(stream, lambdaContext);
    }

    public void Dispose()
    {
        _awsLambdaEntryPoint.Dispose();
    }
}
