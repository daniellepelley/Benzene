using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Microsoft.Extensions.Configuration;

namespace Benzene.Tools.Aws;

public static class AwsLambdaBenzeneTestHostExtensions
{
    public static AwsLambdaBenzeneTestHost BuildHost<TStartUp, TContainer>
        (this AwsLambdaBenzeneTestStartUp<TStartUp, TContainer> source)
        where TStartUp : IStartUp<TContainer, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>
    {
        return new AwsLambdaBenzeneTestHost(source.Build());
    }

    public static AwsLambdaBenzeneTestHost BuildHost(this IAwsEntryPointBuilder source)
    {
        return new AwsLambdaBenzeneTestHost(source.Build());
    }
}
