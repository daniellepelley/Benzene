﻿using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandling;

public class MessageRouterBuilder : IMessageRouterBuilder
{
    private readonly Action<Action<IBenzeneServiceContainer>> _register;
    private readonly List<IHandlerMiddlewareBuilder> _builders;

    public MessageRouterBuilder(IEnumerable<IHandlerMiddlewareBuilder> builders,
        Action<Action<IBenzeneServiceContainer>> register)
    {
        _register = register;
        _builders = builders.ToList();
    }

    public IMessageRouterBuilder Add(IHandlerMiddlewareBuilder handlerMiddlewareBuilder)
    {
        _builders.Add(handlerMiddlewareBuilder);
        return this;
    }

    public IHandlerMiddlewareBuilder[] GetBuilders()
    {
        return _builders.ToArray();
    }

    public void Register(Action<IBenzeneServiceContainer> action)
    {
        _register(action);
    }
}
