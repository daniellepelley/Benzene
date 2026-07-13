using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Core;

/// <summary>AWS Lambda-specific application builder for configuring Lambda pipelines.</summary>
public class AwsLambdaApplicationBuilder : BenzeneApplicationBuilder
{
    /// <summary>The platform name identifier for AWS Lambda.</summary>
    public const string PlatformName = "AwsLambda";

    /// <summary>Initializes a new instance of the <see cref="AwsLambdaApplicationBuilder"/>.</summary>
    /// <param name="eventPipeline">The AWS event stream middleware pipeline builder.</param>
    /// <param name="benzeneServiceContainer">The Benzene service container.</param>
    public AwsLambdaApplicationBuilder(IMiddlewarePipelineBuilder<AwsEventStreamContext> eventPipeline,
        IBenzeneServiceContainer benzeneServiceContainer)
        : base(PlatformName, benzeneServiceContainer)
    {
        EventPipeline = eventPipeline;
    }

    /// <summary>Gets the AWS event stream middleware pipeline builder.</summary>
    public IMiddlewarePipelineBuilder<AwsEventStreamContext> EventPipeline { get; }
}

/// <summary>Extension methods for configuring AWS Lambda-specific settings.</summary>
public static class AwsLambdaApplicationBuilderExtensions
{
    /// <summary>Applies AWS Lambda-specific configuration. No-op on other platforms.</summary>
    public static IBenzeneApplicationBuilder UseAwsLambda(this IBenzeneApplicationBuilder app,
        Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> configure)
    {
        if (app is AwsLambdaApplicationBuilder awsLambda)
        {
            configure(awsLambda.EventPipeline);
        }
        return app;
    }
}
