using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Core.Messages.MessageSender;

public interface IMessageSenderBuilder
{
    void CreateSender<TMessage>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TMessage, Void>>> action);
    void CreateSender<TRequest, TResponse>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>>> action);
}