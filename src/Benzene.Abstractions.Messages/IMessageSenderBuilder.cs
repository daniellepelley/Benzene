using Benzene.Abstractions.Messages.BenzeneClient;
using Void = Benzene.Results.Void;

namespace Benzene.Abstractions.Middleware;

public interface IMessageSenderBuilder
{
    void CreateSender<T>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>>> action);
}