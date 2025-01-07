using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Results;

namespace Benzene.Core.MessageSender;

public interface IMessageSenderBuilder
{
    void CreateSender<TMessage>(System.Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TMessage, Void>>> action);
    void CreateSender<TRequest, TResponse>(System.Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>>> action);
}