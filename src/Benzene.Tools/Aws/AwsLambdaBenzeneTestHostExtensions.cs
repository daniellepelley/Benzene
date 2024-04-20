using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.MiddlewareBuilder;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Tools.Aws;

public static class AwsLambdaBenzeneTestHostExtensions
{
    public static AwsLambdaBenzeneTestHost BuildHost<TStartUp>
        (this AwsLambdaBenzeneTestStartUp<TStartUp> source)
        where TStartUp : IStartUp<IServiceCollection, IMiddlewarePipelineBuilder<AwsEventStreamContext>>
    {
        return new AwsLambdaBenzeneTestHost(source.Build());
    }

    public static AwsLambdaBenzeneTestHost BuildHost(this InlineAwsLambdaStartUp source)
    {
        return new AwsLambdaBenzeneTestHost(source.Build());
    }
}
