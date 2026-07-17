using Benzene.Core.Versioning.Casters;
using Benzene.Core.Versioning.Deserializer;

namespace Benzene.Core.Versioning.Schemas;

public class SchemaCastDefinitionsExpander
{
    public ISchemaCaster[] Expand(ISchemaCaster[] schemaCastDefinitions, PayloadSchemaVersions[] payloadSchemaVersions)
    {
        var expanded = new List<ISchemaCaster>();
        foreach (var def in GetRequiredCasters(payloadSchemaVersions))
        {
            var existingDefinition = schemaCastDefinitions.FirstOrDefault(x =>
                x.Definition.FromSchema == def.FromSchema &&
                x.Definition.ToSchema == def.ToSchema &&
                x.Definition.Topic == def.Topic);

            if (existingDefinition != null)
            {
                expanded.Add(existingDefinition);
                continue;
            }

            var chain = GetChain(schemaCastDefinitions, def.FromSchema, def.ToSchema, def.Topic);
            if (chain.Length > 1)
            {
                expanded.Add(Compose(chain));
            }
        }
        return expanded.ToArray();
    }
    private static SchemaCastDefinition[] GetRequiredCasters(PayloadSchemaVersions[] payloadSchemaVersions)
    {
        var requiredCasters = new List<SchemaCastDefinition>();
        foreach (var definition in payloadSchemaVersions)
        {
            foreach (var fromSchema in definition.FromSchemas)
            {
                foreach (var toSchema in definition.ToSchemas)
                {
                    if (fromSchema != toSchema)
                    {
                        requiredCasters.Add(new SchemaCastDefinition
                        {
                            Topic = definition.Topic,
                            FromSchema = fromSchema,
                            ToSchema = toSchema
                        });
                    }
                }
            }
        }
        return requiredCasters.ToArray();
    }

    private ISchemaCaster[] GetChain(ISchemaCaster[] schemaCastDefinitions, string fromSchema, string toSchema, string topic)
    {
        if (string.Equals(fromSchema, toSchema, StringComparison.Ordinal))
        {
            return [];
        }

        // Build adjacency list for edges that match the requested topic
        var edges = schemaCastDefinitions.Where(e => string.Equals(e.Definition.Topic, topic, StringComparison.Ordinal)).ToList();

        // BFS from fromSchema to find toSchema
        var q = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var prev = new Dictionary<string, string>(StringComparer.Ordinal); // childSchema -> parentSchema
        var via = new Dictionary<string, ISchemaCaster>(StringComparer.Ordinal); // childSchema -> edge used to reach it

        q.Enqueue(fromSchema);
        _ = visited.Add(fromSchema);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();

            // consider all outgoing edges from cur
            foreach (var edge in edges.Where(e => string.Equals(e.Definition.FromSchema, cur, StringComparison.Ordinal)))
            {
                var next = edge.Definition.ToSchema;
                if (visited.Contains(next))
                {
                    continue;
                }

                _ = visited.Add(next);
                prev[next] = cur;
                via[next] = edge;

                if (string.Equals(next, toSchema, StringComparison.Ordinal))
                {
                    // reconstruct chain
                    var stack = new Stack<ISchemaCaster>();
                    var walk = toSchema;
                    while (!string.Equals(walk, fromSchema, StringComparison.Ordinal))
                    {
                        var used = via[walk];
                        stack.Push(used);
                        walk = prev[walk];
                    }
                    return stack.ToArray();
                }

                q.Enqueue(next);
            }
        }

        throw new InvalidOperationException($"No conversion path found for topic='{topic}' from '{fromSchema}' to '{toSchema}'.");
    }

    private static ISchemaCaster Compose(ISchemaCaster[] chain)
    {
        if (chain == null || chain.Length == 0)
        {
            throw new ArgumentException("The chain must contain at least one ISchemaCaster.",
                nameof(chain));
        }

        var current = chain[0];
        for (var i = 1; i < chain.Length; i++)
        {
            var next = chain[i];
            current = Compose(current, next);
        }

        return current;
    }

    private static ISchemaCaster Compose(ISchemaCaster schemaCastDefinition1, ISchemaCaster schemaCastDefinition2)
    {
        var methodInfo = typeof(SchemaCastDefinitionsExpander).GetMethod(nameof(ComposeSchemaCastDefinitions))!;
        var genericMethod = methodInfo.MakeGenericMethod(schemaCastDefinition1.FromType, schemaCastDefinition1.ToType, schemaCastDefinition2.ToType);
        return (ISchemaCaster)genericMethod.Invoke(null, [schemaCastDefinition1, schemaCastDefinition2])!;
    }

    public static SchemaCaster<TFrom, TTo> ComposeSchemaCastDefinitions<TFrom, TIntermediate, TTo>(ISchemaCaster<TFrom, TIntermediate> schemaCastDefinition1, ISchemaCaster<TIntermediate, TTo> schemaCastDefinition2)
    {
        return new SchemaCaster<TFrom, TTo>
        {
            Definition = new SchemaCastDefinition
            {
                Topic = schemaCastDefinition1.Definition.Topic,
                FromSchema = schemaCastDefinition1.Definition.FromSchema,
                ToSchema = schemaCastDefinition2.Definition.ToSchema,
            },
            Caster = new CompositeCaster<TFrom, TIntermediate, TTo>(schemaCastDefinition1.Caster, schemaCastDefinition2.Caster)
        };
    }
}
