using Benzene.Abstractions.DI;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Core.AwsEventStream;

public class AwsEventStreamPipelineBuilder : MiddlewarePipelineBuilder<AwsEventStreamContext>
{
    public AwsEventStreamPipelineBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        : base(benzeneServiceContainer)
    { }
}