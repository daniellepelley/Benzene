using System;
using Benzene.Abstractions.Middleware;
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
    
    public void CreateSender<T>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>>> action)
    {
        var pipeline = _registerDependency.CreateMiddlewarePipeline(action);
        
        _registerDependency.Register(x =>
        {
            x.AddScoped<IMessageSender<T>, MessageSender<T>>();
            x.AddScoped(_ => pipeline);
            x.AddScoped<IGetTopic, DefaultGetTopic>();
        });
    }
}