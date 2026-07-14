using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Presents a handler middleware pipeline (built by <see cref="HandlerPipelineBuilder"/>) as a plain
/// <see cref="IMessageHandler{TRequest,TResponse}"/>, so callers (e.g. <see cref="MessageHandler{TRequest,TResponse}"/>)
/// don't need to know that middleware runs around the handler at all.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request handled by the pipeline.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response produced by the pipeline.</typeparam>
/// <remarks>
/// Each call to <see cref="HandleAsync"/> creates a fresh <see cref="MessageHandlerContext{TRequest,TResponse}"/>
/// and runs it through the pipeline; the pipeline's final <see cref="MessageHandlerMiddleware{TRequest,TResponse}"/>
/// step invokes the actual handler and stores the response back onto that context.
/// </remarks>
public class PipelineMessageHandler<TRequest, TResponse> : IMessageHandler<TRequest, TResponse>
{
    private readonly IMiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>> _pipeline;
    private readonly IServiceResolver _serviceResolver;
    private readonly ITopic _topic;
    private readonly Type? _handlerType;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineMessageHandler{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="topic">The topic this handler is registered for, carried on every invocation's context.</param>
    /// <param name="pipeline">The middleware pipeline to run for each invocation, ending in the actual handler.</param>
    /// <param name="serviceResolver">Resolver passed to the pipeline for resolving per-invocation dependencies.</param>
    /// <param name="handlerType">The concrete handler type, if known, carried on the invocation's context for diagnostics.</param>
    public PipelineMessageHandler(ITopic topic, IMiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>> pipeline, IServiceResolver serviceResolver, Type? handlerType = null)
    {
        _topic = topic;
        _serviceResolver = serviceResolver;
        _pipeline = pipeline;
        _handlerType = handlerType;
    }

    /// <summary>
    /// Runs <paramref name="request"/> through the middleware pipeline and returns the resulting response.
    /// </summary>
    /// <param name="request">The strongly-typed request to handle.</param>
    /// <returns>The response produced by the pipeline (and ultimately the wrapped handler).</returns>
    public async Task<IBenzeneResult<TResponse>> HandleAsync(TRequest request)
    {
        var context = new MessageHandlerContext<TRequest, TResponse>(_topic, request, _handlerType);
        await _pipeline.HandleAsync(context, _serviceResolver);
        return context.Response;
    }
}
