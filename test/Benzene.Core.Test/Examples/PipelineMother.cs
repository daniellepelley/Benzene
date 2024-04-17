using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.DirectMessage;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Test.Examples;

public static class PipelineMother
{
    public static IMiddlewarePipelineBuilder<DirectMessageContext> BasicDirectMessagePipeline(
        IBenzeneServiceContainer serviceContainer)
    {
        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(serviceContainer);

        return pipeline
            .UseProcessResponse()
            .UseMessageRouter();
    }

    public static Action<IMiddlewarePipelineBuilder<DirectMessageContext>> BasicDirectMessagePipeline()
    {
        return pipeline => pipeline
            .UseProcessResponse()
            .UseMessageRouter();
    }
}
