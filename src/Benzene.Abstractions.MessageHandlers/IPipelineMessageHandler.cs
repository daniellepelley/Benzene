using Benzene.Abstractions.Messages;
using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IPipelineMessageHandler<TRequest, TResponse>
{
    Task<IBenzeneResult<TResponse>> HandleAsync(ITopic topic, TRequest request);
}