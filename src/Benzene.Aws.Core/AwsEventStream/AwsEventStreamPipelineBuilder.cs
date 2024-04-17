using Benzene.Abstractions.DI;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Aws.Core.AwsEventStream;

public class AwsEventStreamPipelineBuilder : MiddlewarePipelineBuilder<AwsEventStreamContext>
{
    public AwsEventStreamPipelineBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        : base(benzeneServiceContainer)
    { }
}