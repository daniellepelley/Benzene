using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// The minimal request/response handler contract: given a request, produce a typed result. This is
/// the interface <see cref="IMessageHandler{TRequest, TResponse}"/> extends (with no additional
/// members) so that infrastructure code which only needs to invoke a handler -- without caring
/// whether it was discovered/registered as a full <see cref="IMessageHandler{TRequest, TResponse}"/>
/// -- can depend on this narrower interface instead.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request this handler accepts.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response this handler returns.</typeparam>
public interface IMessageHandlerBase<TRequest, TResponse>
{
    /// <summary>Handles the given request and returns a typed result.</summary>
    /// <param name="request">The strongly-typed request to handle.</param>
    /// <returns>The result of handling the request, including its response payload.</returns>
    Task<IBenzeneResult<TResponse>> HandleAsync(TRequest request);
}