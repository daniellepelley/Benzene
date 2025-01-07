using System;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Core.Middleware;
using Void = Benzene.Results.Void;

namespace Benzene.Core.MessageSender;

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
            x.AddScoped<IMessageSender<TRequest>, MessageSender<TRequest>>();
            x.AddScoped(_ => pipeline);
            x.AddScoped<IGetTopic, DefaultGetTopic>();
        });
    }
    
    public void CreateSender<TRequest, TResponse>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>>> action)
    {
        var pipeline = _registerDependency.CreateMiddlewarePipeline(action);
        
        _registerDependency.Register(x =>
        {
            x.AddScoped<IMessageSender<TRequest, TResponse>, MessageSender<TRequest, TResponse>>();
            x.AddScoped(_ => pipeline);
            x.AddScoped<IGetTopic, DefaultGetTopic>();
        });
    }
}