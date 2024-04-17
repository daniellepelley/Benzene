﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Results;
using Benzene.Results;
using Microsoft.Extensions.Logging;

namespace Benzene.Elements.Core.Broadcast;

public class BroadcastEventMiddleware<TRequest, TResponse> : IMiddleware<IMessageContext<TRequest, TResponse>>
{
    private readonly IServiceResolver _serviceResolver;

    public BroadcastEventMiddleware(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public string Name => "Broadcast Event";

    public async Task HandleAsync(IMessageContext<TRequest, TResponse> context, Func<Task> next)
    {
        var eventBroadcaster = _serviceResolver.Resolve<IEventSender>();
        var logger = _serviceResolver.Resolve<ILogger>();

        await next();

        var topicFunction = TopicFunction(context.Topic);

        if (topicFunction == "create" && context.Response.Status == ServiceResultStatus.Created ||
            topicFunction == "update" && context.Response.Status == ServiceResultStatus.Updated ||
            topicFunction == "delete" && context.Response.Status == ServiceResultStatus.Deleted)
        {
            await eventBroadcaster.SendAsync($"{context.Topic.Id}d", context.Response.Payload);
        }
    }

    public static string TopicFunction(ITopic source)
    {
        return source.Id.Split(":").Last();
    }
}
