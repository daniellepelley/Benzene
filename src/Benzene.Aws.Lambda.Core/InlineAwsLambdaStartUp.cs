using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Provides a fluent, inline alternative to declaring an <see cref="AwsLambdaStartUp"/> subclass —
/// primarily intended for tests and small samples.
/// </summary>
/// <remarks>
/// Uses Microsoft's built-in dependency injection container. For production Lambda functions, prefer
/// deriving from <see cref="AwsLambdaStartUp"/> directly, since it can serve as the Lambda entry point
/// without needing a separate builder step.
/// </remarks>
public class InlineAwsLambdaStartUp : IAwsEntryPointBuilder
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> _appAction = _ => { };

    /// <summary>
    /// Configures the action used to register services with the service collection.
    /// </summary>
    /// <param name="action">The action that registers services.</param>
    /// <returns>This instance for method chaining.</returns>
    public InlineAwsLambdaStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }

    /// <summary>
    /// Configures the action used to build the middleware pipeline.
    /// </summary>
    /// <param name="action">The action that configures the pipeline builder.</param>
    /// <returns>This instance for method chaining.</returns>
    public InlineAwsLambdaStartUp Configure(Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> action)
    {
        _appAction = action;
        return this;
    }

    /// <summary>
    /// Builds the Lambda entry point from the configured actions.
    /// </summary>
    /// <returns>The built <see cref="IAwsLambdaEntryPoint"/>, ready to handle invocations.</returns>
    public IAwsLambdaEntryPoint Build()
    {
        var services = new ServiceCollection();
        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(services));

        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        return new AwsLambdaEntryPoint(app.Build(), serviceResolverFactory);
    }
}
