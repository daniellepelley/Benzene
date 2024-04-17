﻿using Benzene.Abstractions.MessageHandling;

namespace Benzene.DataAnnotations;

public static class DependencyExtensions
{
    public static IMessageRouterBuilder UseDataAnnotationsValidation(this IMessageRouterBuilder builder)
    {
        return builder.Add(new ValidationMiddlewareBuilder());
    }
}
