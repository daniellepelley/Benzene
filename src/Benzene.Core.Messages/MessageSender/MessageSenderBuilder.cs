using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Core.Messages.MessageSender;

public class MessageSenderBuilder : IMessageSenderBuilder
{
    private readonly IRegisterDependency _registerDependency;

    public MessageSenderBuilder(IRegisterDependency registerDependency)
    {
        _registerDependency = registerDependency;
    }
    
    public void CreateSender<TRequest>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, Void>>> action)
    {
        var pipeline = _registerDependency.CreateMiddlewarePipeline(action);
        
        _registerDependency.Register(x =>
        {
            x.TryAddScoped<IMessageSender<TRequest>, MessageSender<TRequest>>();
            x.TryAddScoped(_ => pipeline);
            x.TryAddScoped<IGetTopic, DefaultGetTopic>();
        });
    }
    
    public void CreateSender<TRequest, TResponse>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>>> action)
    {
        var pipeline = _registerDependency.CreateMiddlewarePipeline(action);
        
        _registerDependency.Register(x =>
        {
            x.TryAddScoped<IMessageSender<TRequest, TResponse>, MessageSender<TRequest, TResponse>>();
            x.TryAddScoped(_ => pipeline);
            x.TryAddScoped<IGetTopic, DefaultGetTopic>();
        });
    }
}