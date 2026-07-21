using System;

namespace Benzene.Avro;

/// <summary>
/// Thrown when an Avro binary body declares a length-prefixed field (bytes/string/array block) larger
/// than the deserializer will allocate - either bigger than the whole decoded input (impossible for a
/// legitimate field) or bigger than <see cref="AvroOptions.MaxDeserializeBytes"/>. Rejecting it up
/// front stops a hostile payload from driving a large allocation before any data is read.
/// </summary>
public class AvroPayloadTooLargeException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="declaredLength">The field length the payload declared.</param>
    /// <param name="maxLength">The maximum length the deserializer will accept.</param>
    public AvroPayloadTooLargeException(long declaredLength, long maxLength)
        : base($"Avro payload declares a {declaredLength}-byte field, exceeding the maximum of {maxLength} bytes the deserializer will allocate.")
    {
        DeclaredLength = declaredLength;
        MaxLength = maxLength;
    }

    /// <summary>The field length the payload declared.</summary>
    public long DeclaredLength { get; }

    /// <summary>The maximum length the deserializer will accept.</summary>
    public long MaxLength { get; }
}
