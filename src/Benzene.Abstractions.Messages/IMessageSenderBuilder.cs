using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Abstractions.Messages;

public interface IMessageSenderBuilder
{
    void CreateSender<T>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>>> action);
}