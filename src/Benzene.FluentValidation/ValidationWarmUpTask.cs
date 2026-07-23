using System;
using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.WarmUp;
using FluentValidation;

namespace Benzene.FluentValidation;

/// <summary>
/// Warms FluentValidation by constructing the <see cref="IValidator{T}"/> for each registered handler's
/// request type. A FluentValidation <c>AbstractValidator</c> builds its entire rule set in its
/// constructor, and validators are registered as singletons, so resolving each one once at start-up
/// JITs and pre-builds that rule set - keeping it off the first real message (the other of the two
/// ~18ms first-message gaps in the AWS X-Ray cold-start analysis).
/// </summary>
public class ValidationWarmUpTask : IWarmUpTask
{
    // IServiceResolver only resolves by static generic type; the request type is only known at runtime,
    // so reach IValidator<T> through the generic TryGetService<T> via reflection (INIT-phase, one-off).
    private static readonly MethodInfo TryGetServiceMethod =
        typeof(IServiceResolver).GetMethod(nameof(IServiceResolver.TryGetService))!;

    /// <inheritdoc />
    public void WarmUp(IServiceResolver resolver)
    {
        var finder = resolver.TryGetService<IMessageHandlersFinder>();
        if (finder is null)
        {
            return;
        }

        foreach (var definition in finder.FindDefinitions())
        {
            WarmValidator(resolver, definition.RequestType);
        }
    }

    private static void WarmValidator(IServiceResolver resolver, Type requestType)
    {
        try
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
            // Resolving constructs the singleton validator, whose ctor builds its whole rule set.
            TryGetServiceMethod.MakeGenericMethod(validatorType).Invoke(resolver, null);
        }
        catch
        {
            // Best-effort - no validator registered for this type, or a construction hiccup.
        }
    }
}
