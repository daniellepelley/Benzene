using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Serialization;
using Benzene.Abstractions.WarmUp;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Core.MessageHandlers.WarmUp;

/// <summary>
/// Warms the serializer's per-type machinery - e.g. System.Text.Json builds and caches reflection
/// metadata and a converter for each type on its first (de)serialize - for every registered handler's
/// request AND response type, so the first real message of each type doesn't pay that build. This was
/// one of the two ~18ms first-message gaps in the AWS X-Ray cold-start analysis. Warming the response
/// type as well matters for read-shaped topics (a trivial request but a large response payload), where a
/// later cold-start trace showed the response serialization - not the request deserialization - was the
/// dominant unwarmed first-message cost.
/// </summary>
public class SerializationWarmUpTask : IWarmUpTask
{
    /// <inheritdoc />
    public void WarmUp(IServiceResolver resolver)
    {
        var finder = resolver.TryGetService<IMessageHandlersFinder>();
        if (finder is null)
        {
            return;
        }

        // The negotiated request/response path deserializes with the concrete JsonSerializer (the one
        // JsonMediaFormat binds to), which is a DIFFERENT instance - with its own STJ metadata cache -
        // from the ISerializer registration. Warm both: the concrete one pre-builds the hot path's
        // per-type metadata, and any distinct/custom ISerializer is warmed too. Warming either also JITs
        // the shared serializer machinery.
        var serializers = new List<ISerializer>();
        var concrete = resolver.TryGetService<JsonSerializer>();
        if (concrete is not null)
        {
            serializers.Add(concrete);
        }

        var abstraction = resolver.TryGetService<ISerializer>();
        if (abstraction is not null && !ReferenceEquals(abstraction, concrete))
        {
            serializers.Add(abstraction);
        }

        if (serializers.Count == 0)
        {
            return;
        }

        // Collect the distinct request AND response types across all handlers, then warm each once per
        // serializer. Deduping matters because request/response types are commonly shared across handlers
        // (and a request can equal a response), so a flat per-definition loop would re-pay the same type.
        var types = new HashSet<Type>();
        foreach (var definition in finder.FindDefinitions())
        {
            if (definition.RequestType is not null)
            {
                types.Add(definition.RequestType);
            }

            // The response path serializes this type when writing the reply; for read-shaped topics it's
            // the bigger first-message cost, so warm it too (a round-trip warms both read and write metadata).
            if (definition.ResponseType is not null)
            {
                types.Add(definition.ResponseType);
            }
        }

        foreach (var type in types)
        {
            foreach (var serializer in serializers)
            {
                WarmType(serializer, type);
            }
        }
    }

    private static void WarmType(ISerializer serializer, Type requestType)
    {
        try
        {
            // Deserializing an empty object builds and caches the per-type converter/metadata even if
            // the input is ultimately rejected (required members etc.) - the type-specific code has run
            // by then. The round-trip also warms the serialize path. We only care that it ran.
            var instance = serializer.Deserialize(requestType, "{}");
            if (instance is not null)
            {
                serializer.Serialize(requestType, instance);
            }
        }
        catch
        {
            // Best-effort warm-up: the JIT/metadata build already happened before any rejection.
        }
    }
}
