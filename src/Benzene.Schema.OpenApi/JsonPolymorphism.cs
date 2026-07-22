using System.Reflection;
using System.Text.Json.Serialization;

namespace Benzene.Schema.OpenApi;

/// <summary>
/// Reads System.Text.Json's polymorphism attributes (<c>[JsonPolymorphic]</c>/<c>[JsonDerivedType]</c>)
/// off payload models. These are <see cref="SchemaGenerationOptions"/>' default resolvers, so schemas
/// generated with polymorphism enabled describe exactly the union/discriminator behavior the default
/// runtime serializer applies to the same models.
/// </summary>
public static class JsonPolymorphism
{
    /// <summary>The derived types a base type declares via <c>[JsonDerivedType]</c>.</summary>
    /// <param name="type">The base type.</param>
    /// <returns>The declared derived types; empty when the type declares none.</returns>
    public static IEnumerable<Type> GetDerivedTypes(Type type)
    {
        return type.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false)
            .Select(x => x.DerivedType);
    }

    /// <summary>
    /// The discriminator property name for a base type: <c>[JsonPolymorphic]</c>'s
    /// <c>TypeDiscriminatorPropertyName</c>, or STJ's default <c>$type</c> when the type declares
    /// derived types with discriminator values but no explicit property name. <c>null</c> when the
    /// type declares no discriminated derived types at all.
    /// </summary>
    /// <param name="type">The base type.</param>
    public static string? GetDiscriminatorName(Type type)
    {
        var hasDiscriminatedDerivedTypes = type.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false)
            .Any(x => x.TypeDiscriminator != null);
        if (!hasDiscriminatedDerivedTypes)
        {
            return null;
        }

        return type.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false)
            ?.TypeDiscriminatorPropertyName ?? "$type";
    }

    /// <summary>
    /// The discriminator value a derived type serializes as: the <c>TypeDiscriminator</c> of the
    /// <c>[JsonDerivedType]</c> naming it on a base class or implemented interface. <c>null</c>
    /// when no ancestor declares one.
    /// </summary>
    /// <param name="derivedType">The derived type.</param>
    public static string? GetDiscriminatorValue(Type derivedType)
    {
        for (var baseType = derivedType.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            var value = FindDiscriminator(baseType, derivedType);
            if (value != null)
            {
                return value;
            }
        }

        return derivedType.GetInterfaces()
            .Select(x => FindDiscriminator(x, derivedType))
            .FirstOrDefault(x => x != null);
    }

    private static string? FindDiscriminator(Type declaringType, Type derivedType)
    {
        return declaringType.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false)
            .FirstOrDefault(x => x.DerivedType == derivedType && x.TypeDiscriminator != null)
            ?.TypeDiscriminator?.ToString();
    }
}
