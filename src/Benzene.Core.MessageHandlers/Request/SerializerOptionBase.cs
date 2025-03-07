﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

public abstract class SerializerOptionBase<TContext, TSerializer> : ISerializerOption<TContext> where TSerializer : class, ISerializer
{
    public abstract ISerializer GetSerializer(IServiceResolver serviceResolver);

    public abstract IContextPredicate<TContext> CanHandle { get; }
}