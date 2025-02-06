using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Messages.MessageSender;

public interface IMessageSenderBuilder
{
    void CreateSender<TMessage>(System.Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TMessage, Results.Void>>> action);
    void CreateSender<TRequest, TResponse>(System.Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>>> action);
}