using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Test.Examples;

public static class PipelineMother
{
    public static IMiddlewarePipelineBuilder<BenzeneMessageContext> BasicBenzeneMessagePipeline(
        IBenzeneServiceContainer serviceContainer)
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(serviceContainer);

        return pipeline
            .UseProcessResponse()
            .UseMessageRouter();
    }

    public static Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> BasicBenzeneMessagePipeline()
    {
        return pipeline => pipeline
            .UseProcessResponse()
            .UseMessageRouter();
    }
}
