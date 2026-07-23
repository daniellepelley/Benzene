using Benzene.Abstractions.DI;
using Benzene.Abstractions.WarmUp;

namespace Benzene.Core.MessageHandlers.WarmUp;

/// <summary>Marker singleton whose presence means cold-start warm-up was opted into.</summary>
internal sealed class BenzeneWarmUpMarker
{
}

/// <summary>Opt-in registration and the host-side runner for cold-start warm-up.</summary>
public static class BenzeneWarmUpExtensions
{
    /// <summary>
    /// Opts the service into cold-start warm-up: registers the core serialization warm-up task and
    /// marks warm-up enabled, so a host's <see cref="WarmUp(IServiceResolverFactory)"/> runs it (and any
    /// other registered <see cref="IWarmUpTask"/> - e.g. FluentValidation's validator warm-up). Off
    /// unless this is called - and even when called it does no message dispatch, so it stays invisible.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddBenzeneWarmUp(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<BenzeneWarmUpMarker>();
        services.AddSingleton<IWarmUpTask, SerializationWarmUpTask>();
        return services;
    }

    /// <summary>
    /// Runs every registered <see cref="IWarmUpTask"/> once, on a throwaway scope, <b>if</b> warm-up was
    /// opted into via <see cref="AddBenzeneWarmUp"/> (a no-op otherwise). A host calls this from its
    /// initialization - i.e. the Lambda/Functions INIT phase - so the cost is paid off the request path
    /// and the first real invocation is already warm. Never throws: a failed warm just means that cost
    /// falls back onto the first message.
    /// </summary>
    /// <param name="factory">The service resolver factory to warm through.</param>
    public static void WarmUp(this IServiceResolverFactory factory)
    {
        using var resolver = factory.CreateScope();
        if (resolver.TryGetService<BenzeneWarmUpMarker>() is null)
        {
            return;
        }

        foreach (var task in resolver.GetServices<IWarmUpTask>())
        {
            try
            {
                task.WarmUp(resolver);
            }
            catch
            {
                // Warm-up must never break start-up; skip a failing task and carry on.
            }
        }
    }
}
