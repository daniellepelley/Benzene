using System;

namespace Benzene.Aws.Lambda.S3;

/// <summary>
/// Thrown by <see cref="S3Application"/> when <see cref="S3Options.RaiseOnFailureStatus"/> is enabled
/// and a message handler reported an unsuccessful result without itself throwing - escalating the
/// failure into an exception so S3's async-invoke retry applies the same way it would for an
/// unhandled exception.
/// </summary>
public class S3MessageProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="S3MessageProcessingException"/> class.
    /// </summary>
    /// <param name="objectKey">The S3 object key the handler reported a failure for.</param>
    public S3MessageProcessingException(string objectKey)
        : base($"Message handler reported an unsuccessful result for S3 object {objectKey}.")
    {
        ObjectKey = objectKey;
    }

    /// <summary>Gets the S3 object key the handler reported a failure for.</summary>
    public string ObjectKey { get; }
}
