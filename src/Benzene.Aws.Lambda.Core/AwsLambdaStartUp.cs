using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Provides the base class for configuring an AWS Lambda function using Microsoft's built-in
/// dependency injection container.
/// </summary>
/// <remarks>
/// Inherit from this class to define your Lambda's <c>StartUp</c>. Because it directly implements
/// <see cref="IAwsLambdaEntryPoint"/>, your subclass itself is the Lambda entry point — point your
/// <c>function-handler</c> configuration at <c>YourAssembly::YourNamespace.YourStartUp::FunctionHandlerAsync</c>,
/// with no separate entry point class required.
/// </remarks>
[Obsolete("Superseded by the platform-neutral BenzeneStartUp hosted via AwsLambdaHost<TStartUp>, whose Configure takes IBenzeneApplicationBuilder (use UseAwsLambda(...) to wire the event pipeline). See docs/migration-alpha-to-1.0.md.")]
public abstract class AwsLambdaStartUp : AwsLambdaStartUp<IServiceCollection>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaStartUp"/> class, using Microsoft's
    /// built-in dependency injection container.
    /// </summary>
    protected AwsLambdaStartUp()
        : base(new MicrosoftDependencyInjectionAdapter())
    { }
}

/// <summary>
/// Provides the base class for configuring an AWS Lambda function with a pluggable dependency
/// injection container type.
/// </summary>
/// <typeparam name="TContainer">The dependency injection container type (e.g. <see cref="IServiceCollection"/> or Autofac's <c>ContainerBuilder</c>).</typeparam>
/// <remarks>
/// On construction, this class calls <see cref="GetConfiguration"/>, <see cref="ConfigureServices"/>,
/// and <see cref="Configure"/> (in that order) to build the middleware pipeline once, then reuses it
/// for every subsequent invocation via <see cref="FunctionHandlerAsync"/>.
/// </remarks>
[Obsolete("Superseded by the platform-neutral BenzeneStartUp hosted via AwsLambdaHost<TStartUp>, whose Configure takes IBenzeneApplicationBuilder (use UseAwsLambda(...) to wire the event pipeline). See docs/migration-alpha-to-1.0.md.")]
public abstract class AwsLambdaStartUp<TContainer> : IStartUp<TContainer, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>, IAwsLambdaEntryPoint
{
    private readonly AwsLambdaEntryPoint _awsLambdaEntryPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaStartUp{TContainer}"/> class, building
    /// the middleware pipeline and service container immediately.
    /// </summary>
    /// <param name="dependencyInjectionAdapter">The adapter that creates and wraps the underlying dependency injection container.</param>
    protected AwsLambdaStartUp(IDependencyInjectionAdapter<TContainer> dependencyInjectionAdapter)
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        var configuration = GetConfiguration();
        var services = dependencyInjectionAdapter.CreateContainer();
        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(dependencyInjectionAdapter.CreateBenzeneServiceContainer(services));

        // ReSharper disable once VirtualMemberCallInConstructor
        ConfigureServices(services, configuration);

        // ReSharper disable once VirtualMemberCallInConstructor
        Configure(app, configuration);
        var pipeline = app.Build();

        _awsLambdaEntryPoint = new AwsLambdaEntryPoint(pipeline, dependencyInjectionAdapter.CreateBenzeneServiceResolverFactory(services));
    }

    /// <summary>
    /// Builds the configuration for this Lambda function.
    /// </summary>
    /// <returns>The configuration to use for service registration and pipeline setup.</returns>
    public abstract IConfiguration GetConfiguration();

    /// <summary>
    /// Registers services with the dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to register services with.</param>
    /// <param name="configuration">The configuration built by <see cref="GetConfiguration"/>.</param>
    public abstract void ConfigureServices(TContainer services, IConfiguration configuration);

    /// <summary>
    /// Configures the middleware pipeline for this Lambda function.
    /// </summary>
    /// <param name="app">The pipeline builder to configure, typically by adding event source middleware such as API Gateway, SQS, or SNS.</param>
    /// <param name="configuration">The configuration built by <see cref="GetConfiguration"/>.</param>
    public abstract void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration);

    /// <summary>
    /// Handles a single AWS Lambda invocation by delegating to the built middleware pipeline.
    /// </summary>
    /// <param name="stream">The raw Lambda invocation payload stream.</param>
    /// <param name="lambdaContext">The AWS Lambda execution context for this invocation.</param>
    /// <returns>A task that resolves to the response stream written by the pipeline.</returns>
    public Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext lambdaContext)
    {
        return _awsLambdaEntryPoint.FunctionHandlerAsync(stream, lambdaContext);
    }

    /// <summary>
    /// Disposes the underlying Lambda entry point and its service resolver factory.
    /// </summary>
    public void Dispose()
    {
        _awsLambdaEntryPoint.Dispose();
    }
}
