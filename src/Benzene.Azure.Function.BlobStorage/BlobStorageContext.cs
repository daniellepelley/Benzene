namespace Benzene.Azure.Function.BlobStorage;

/// <summary>
/// Provides the middleware pipeline context for a single blob within an Azure Functions blob trigger
/// invocation.
/// </summary>
public class BlobStorageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageContext"/> class.
    /// </summary>
    /// <param name="blob">The delivered blob.</param>
    public BlobStorageContext(BlobTriggerEvent blob)
    {
        Blob = blob;
    }

    /// <summary>
    /// Gets the delivered blob.
    /// </summary>
    public BlobTriggerEvent Blob { get; }
}
