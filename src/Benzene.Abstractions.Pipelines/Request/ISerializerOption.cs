﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.Request;

public interface ISerializerOption<TContext>
{
    IContextPredicate<TContext> CanHandle { get; }
    ISerializer GetSerializer(IServiceResolver serviceResolver);
}