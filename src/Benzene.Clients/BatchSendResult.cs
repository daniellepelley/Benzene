using System.Collections.Generic;

namespace Benzene.Clients;

/// <summary>
/// The outcome of an <see cref="IBenzeneBatchMessageClient.SendBatchAsync{TRequest}"/> call: the
/// entries that failed to send, identified by their index in the original request collection.
/// </summary>
public class BatchSendResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchSendResult"/> class.
    /// </summary>
    /// <param name="failures">The failed entries; empty when every message sent successfully.</param>
    public BatchSendResult(IReadOnlyList<FailedBatchEntry> failures)
    {
        Failures = failures;
    }

    /// <summary>Gets the entries that failed to send, keyed by their index in the request collection.</summary>
    public IReadOnlyList<FailedBatchEntry> Failures { get; }

    /// <summary>Gets whether every message in the batch was sent successfully.</summary>
    public bool AllSucceeded => Failures.Count == 0;
}

/// <summary>
/// A single failed entry within a batch send: its index in the original request collection plus the
/// provider's error code/message, so the caller can identify and retry exactly which message failed.
/// </summary>
public class FailedBatchEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedBatchEntry"/> class.
    /// </summary>
    /// <param name="index">The failed request's index in the original request collection.</param>
    /// <param name="errorCode">The provider's error code, if any.</param>
    /// <param name="errorMessage">The provider's error message, if any.</param>
    public FailedBatchEntry(int index, string? errorCode, string? errorMessage)
    {
        Index = index;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets the failed request's index in the original request collection.</summary>
    public int Index { get; }

    /// <summary>Gets the provider's error code, if any.</summary>
    public string? ErrorCode { get; }

    /// <summary>Gets the provider's error message, if any.</summary>
    public string? ErrorMessage { get; }
}
