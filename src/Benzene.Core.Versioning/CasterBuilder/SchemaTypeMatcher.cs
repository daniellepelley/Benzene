using System.Reflection;

namespace Benzene.Core.Versioning.CasterBuilder;

/// <summary>
/// Resolves which concrete type in the target schema a source type should be mapped to.
/// Explicit registrations take precedence; otherwise a type with the same name is looked up
/// in the target type's namespace and assembly.
/// </summary>
public class SchemaTypeMatcher(IReadOnlyDictionary<Type, Type> typeMapping)
{
    /// <summary>
    /// Finds the target type for <paramref name="fromType"/>, or null when no explicit mapping
    /// or same-named assignable type exists in the target namespace.
    /// </summary>
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
            // swallow any reflection errors and report no match
        }

        return null;
    }

    /// <summary>
    /// Finds the target type for <paramref name="fromType"/>, falling back to
    /// <paramref name="toType"/> itself when no match exists.
    /// </summary>
    public Type TryGetType(Type fromType, Type toType)
    {
        return GetType(fromType, toType) ?? toType;
    }
}
