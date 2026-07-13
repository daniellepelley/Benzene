namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// The kinds of change the <see cref="SchemaCompatibilityComparer"/> detects between two versions of
/// a service's schema. Each kind is classified into a <see cref="ChangeCompatibility"/> by the
/// configured <see cref="SchemaCompatibilityRules"/>, taking the <see cref="SchemaDirection"/> into
/// account.
/// </summary>
public enum SchemaChangeKind
{
    /// <summary>A topic exists on the service that the client did not know about.</summary>
    TopicAdded,

    /// <summary>A topic the client expects is no longer served.</summary>
    TopicRemoved,

    /// <summary>An optional property was added to a type.</summary>
    PropertyAdded,

    /// <summary>A required property was added to a type.</summary>
    RequiredPropertyAdded,

    /// <summary>A property was removed from a type.</summary>
    PropertyRemoved,

    /// <summary>An existing property changed from optional to required.</summary>
    PropertyBecameRequired,

    /// <summary>An existing property changed from required to optional.</summary>
    PropertyBecameOptional,

    /// <summary>A property's data type or format changed.</summary>
    TypeChanged
}
