using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;

namespace Benzene.Test.Examples;

public static class PipelineMother
{
    public static IMiddlewarePipelineBuilder<BenzeneMessageContext> BasicBenzeneMessagePipeline(
        IBenzeneServiceContainer serviceContainer)
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(serviceContainer);

        return pipeline
            .UseProcessResponse()
            .UseMessageHandlers();
    }

    public static Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> BasicBenzeneMessagePipeline()
    {
        return pipeline => pipeline
            .UseProcessResponse()
            .UseMessageHandlers();
    }
}
