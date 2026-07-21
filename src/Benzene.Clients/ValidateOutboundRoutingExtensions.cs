using System.Reflection;
using Benzene.Abstractions.DI;

namespace Benzene.Clients;

/// <summary>
/// The compile-time-adjacent safety net from <c>work/benzene-clients-redesign-plan.md</c> §2.5:
/// catches a missing outbound route at startup instead of the first time a rarely-hit code path
/// executes it.
/// </summary>
public static class ValidateOutboundRoutingExtensions
{
    /// <summary>
    /// Reflects over every loaded assembly for a type carrying
    /// <see cref="OutboundRoutingContractAttribute"/> and exposing a public static
    /// <c>string[] RequiredTopics</c> field (emitted by <c>Benzene.CodeGen.Client</c>'s generated
    /// clients) and throws <see cref="MissingOutboundRoutesException"/> listing every required
    /// topic with no route registered via <see cref="OutboundRoutingBuilder.Route"/>. Call once,
    /// typically right after <c>AddOutboundRouting(...)</c> resolves. Entirely opt-in - an app with
    /// no generated clients, or that prefers the runtime <see cref="UnroutedTopicException"/>
    /// fallback, simply doesn't call this.
    /// </summary>
    /// <param name="serviceResolver">Resolves the <see cref="OutboundRoutingTopics"/> registered by <c>AddOutboundRouting(...)</c>.</param>
    /// <exception cref="MissingOutboundRoutesException">One or more required topics have no registered outbound route.</exception>
    public static void ValidateOutboundRouting(this IServiceResolver serviceResolver)
    {
        var registeredTopics = serviceResolver.GetService<OutboundRoutingTopics>().Topics;

        var missingTopics = DiscoverRequiredTopics(AppDomain.CurrentDomain.GetAssemblies())
            .Where(topic => !registeredTopics.Contains(topic))
            .Distinct()
            .OrderBy(topic => topic, StringComparer.Ordinal)
            .ToArray();

        if (missingTopics.Length > 0)
        {
            throw new MissingOutboundRoutesException(missingTopics);
        }
    }

    private static IEnumerable<string> DiscoverRequiredTopics(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            Type?[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            foreach (var type in types)
            {
                if (type?.GetCustomAttribute<OutboundRoutingContractAttribute>() == null)
                {
                    continue;
                }

                var field = type.GetField("RequiredTopics", BindingFlags.Public | BindingFlags.Static);
                if (field?.GetValue(null) is string[] topics)
                {
                    foreach (var topic in topics)
                    {
                        yield return topic;
                    }
                }
            }
        }
    }
}
