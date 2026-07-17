using System.Reflection;

namespace Benzene.Core.Versioning.CasterBuilder;

public class SchemaTypeMatcher(IReadOnlyDictionary<Type, Type> typeMapping)
{
    public Type? GetType(Type fromType, Type toType)
    {
        if (typeMapping.TryGetValue(fromType, out var type))
        {
            return type;
        }

        var sourceName = fromType.Name.Split('`')[0];

        try
        {
            var asm = toType.Assembly;

            Type[] types;
            try
            {
                types = asm.GetTypes().Where(x => x.Namespace == toType.Namespace).ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var t in types)
            {
                if (t.IsAbstract)
                {
                    continue;
                }

                if (!toType.IsAssignableFrom(t))
                {
                    continue;
                }

                var candidateName = t.Name.Split('`')[0];
                if (string.Equals(candidateName, sourceName, StringComparison.Ordinal))
                {
                    return t;
                }
            }
        }
        catch
        {
            // swallow any reflection errors and fall back to toType
        }

        return null;
    }

    public Type TryGetType(Type fromType, Type toType)
    {
        if (typeMapping.TryGetValue(fromType, out var type))
        {
            return type;
        }

        var sourceName = fromType.Name.Split('`')[0];

        try
        {
            var asm = toType.Assembly;

            Type[] types;
            try
            {
                types = asm.GetTypes().Where(x => x.Namespace == toType.Namespace).ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var t in types)
            {
                if (t.IsAbstract)
                {
                    continue;
                }

                if (!toType.IsAssignableFrom(t))
                {
                    continue;
                }

                var candidateName = t.Name.Split('`')[0];
                if (string.Equals(candidateName, sourceName, StringComparison.Ordinal))
                {
                    return t;
                }
            }
        }
        catch
        {
            // swallow any reflection errors and fall back to toType
        }

        return toType;
    }
}
