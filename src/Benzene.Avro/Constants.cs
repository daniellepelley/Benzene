namespace Benzene.Avro;

/// <summary>
/// Shared constants for the Avro media format.
/// </summary>
public static class Constants
{
    /// <summary>The request/response header carrying the media type.</summary>
    public const string ContentTypeHeader = "content-type";

    /// <summary>
    /// The content type this format reads and writes. <c>application/avro</c> is the conventional
    /// media type for a bare Avro binary payload (no container/schema envelope).
    /// </summary>
    public const string AvroContentType = "application/avro";
}
