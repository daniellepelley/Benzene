using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Aws.Core;

public abstract class AwsLambdaStartUp : AwsLambdaStartUp<IServiceCollection>
{
    protected AwsLambdaStartUp()
        : base(new MicrosoftDependencyInjectionAdapter())
    { }
}

public abstract class AwsLambdaStartUp<TContainer> : IStartUp<TContainer, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>, IAwsLambdaEntryPoint
{
    private readonly AwsLambdaEntryPoint _awsLambdaEntryPoint;

    protected AwsLambdaStartUp(IDependencyInjectionAdapter<TContainer> dependencyInjectionAdapter)
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        var configuration = GetConfiguration();
        var services = dependencyInjectionAdapter.CreateContainer();
        var app = new AwsEventStreamPipelineBuilder(dependencyInjectionAdapter.CreateBenzeneServiceContainer(services));

        // ReSharper disable once VirtualMemberCallInConstructor
        ConfigureServices(services, configuration);

        // ReSharper disable once VirtualMemberCallInConstructor
        Configure(app, configuration);
        var pipeline = app.Build();

        _awsLambdaEntryPoint = new AwsLambdaEntryPoint(pipeline, dependencyInjectionAdapter.CreateBenzeneServiceResolverFactory(services));
    }

    public abstract IConfiguration GetConfiguration();
    public abstract void ConfigureServices(TContainer services, IConfiguration configuration);

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

