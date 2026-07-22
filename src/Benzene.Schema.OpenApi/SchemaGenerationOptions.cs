namespace Benzene.Schema.OpenApi;

/// <summary>
/// Opt-in controls for how <see cref="SchemaBuilder"/> renders inheritance and polymorphism in
/// generated schemas. With no options registered the output is unchanged: flattened object schemas
/// (base properties inlined into each type, base-typed members rendered as the base only).
/// Register via <see cref="Extensions.SetSchemaGenerationOptions"/>.
/// </summary>
/// <remarks>
/// Subtype and discriminator resolution defaults to the model's own System.Text.Json polymorphism
/// attributes (<c>[JsonDerivedType]</c>/<c>[JsonPolymorphic]</c>, see <see cref="JsonPolymorphism"/>),
/// so the emitted contract matches what the default runtime serializer actually does. The resolver
/// hooks exist for hierarchies that can't carry the attributes.
/// </remarks>
public class SchemaGenerationOptions
{
    /// <summary>
    /// When <c>true</c>, a derived type's schema is emitted as <c>allOf</c> [base <c>$ref</c> +
    /// its own properties] instead of a flattened copy of every inherited property.
    /// </summary>
    public bool UseAllOfForInheritance { get; set; }

    /// <summary>
    /// When <c>true</c>, a type with known subtypes is emitted as <c>oneOf</c> its subtype schemas
    /// (with a <c>discriminator</c> when the model declares one), so derived-only members appear
    /// in the contract wherever the base type is used.
    /// </summary>
    public bool UseOneOfForPolymorphism { get; set; }

    /// <summary>
    /// Resolves the known subtypes of a base type. Default: the base's
    /// <c>[JsonDerivedType]</c> attributes (<see cref="JsonPolymorphism.GetDerivedTypes"/>).
    /// </summary>
    public Func<Type, IEnumerable<Type>>? SubTypesResolver { get; set; }

    /// <summary>
    /// Resolves the discriminator property name for a base type, or <c>null</c> for none.
    /// Default: <c>[JsonPolymorphic].TypeDiscriminatorPropertyName</c>, falling back to STJ's
    /// <c>$type</c> when the base declares derived types without naming the property
    /// (<see cref="JsonPolymorphism.GetDiscriminatorName"/>).
    /// </summary>
    public Func<Type, string?>? DiscriminatorNameResolver { get; set; }

    /// <summary>
    /// Resolves the discriminator value for a derived type, or <c>null</c> for none. Default: the
    /// <c>[JsonDerivedType]</c> type discriminator declared on its base
    /// (<see cref="JsonPolymorphism.GetDiscriminatorValue"/>).
    /// </summary>
    public Func<Type, string?>? DiscriminatorValueResolver { get; set; }
}
