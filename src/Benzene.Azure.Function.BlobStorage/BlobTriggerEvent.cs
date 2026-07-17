using System.Text;

namespace Benzene.Azure.Function.BlobStorage;

/// <summary>
/// Benzene's own model of a blob trigger delivery - dependency-free, mirroring how
/// <c>Benzene.Aws.Lambda.Kinesis</c> models its Lambda event. Built from what the isolated-worker
/// <c>BlobTrigger</c> hands the function: the blob's content (bind <c>byte[]</c>) and its name (bind
/// the <c>{name}</c> expression as a <c>string</c> parameter).
/// </summary>
public class BlobTriggerEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobTriggerEvent"/> class.
    /// </summary>
    /// <param name="name">The blob's name (the trigger's <c>{name}</c> binding expression value).</param>
    /// <param name="content">The blob's content.</param>
    public BlobTriggerEvent(string name, byte[] content)
    {
        Name = name;
        Content = content;
    }

    /// <summary>The blob's name within the triggering container.</summary>
    public string Name { get; }

    /// <summary>The blob's content.</summary>
    public byte[] Content { get; }

    /// <summary>
    /// Decodes <see cref="Content"/> as a UTF-8 string - for text blobs (JSON, CSV, ...).
    /// </summary>
    /// <returns>The content as a string.</returns>
    public string GetContentAsString()
    {
        return Encoding.UTF8.GetString(Content);
    }
}
