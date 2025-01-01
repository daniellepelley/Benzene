using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Core.MessageSender;

public interface IMessageSenderBuilder
{
    void CreateSender<T>(System.Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>>> action);
}