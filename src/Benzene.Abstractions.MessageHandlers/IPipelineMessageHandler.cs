using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// A handler that has already been wrapped with its middleware pipeline (see
/// <see cref="IHandlerPipelineBuilder"/>) and is invoked with the topic in addition to the request,
/// since the pipeline's <see cref="IMessageHandlerContext{TRequest, TResponse}"/> needs the topic
/// for logging/routing metadata even though the plain <see cref="IMessageHandler{TRequest, TResponse}"/>
/// contract does not carry it.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request this handler accepts.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response this handler returns.</typeparam>
public interface IPipelineMessageHandler<TRequest, TResponse>
{
    /// <summary>Runs the request through the handler's middleware pipeline and returns its result.</summary>
    /// <param name="topic">The topic the request was routed on.</param>
    /// <param name="request">The strongly-typed request to handle.</param>
    /// <returns>The result of handling the request.</returns>
    Task<IBenzeneResult<TResponse>> HandleAsync(ITopic topic, TRequest request);
}