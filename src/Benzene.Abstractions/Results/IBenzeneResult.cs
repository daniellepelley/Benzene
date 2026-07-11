namespace Benzene.Abstractions.Results;

/// <summary>
/// Represents the result of a Benzene operation with status, errors, and a payload.
/// This interface enables consistent result handling across different handler types and middleware.
/// </summary>
public interface IBenzeneResult
{
    /// <summary>
    /// Gets the status code or status identifier for the result (e.g., "200", "404", "Success", "NotFound").
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    bool IsSuccessful { get; }

    /// <summary>
    /// Gets the payload as an object for non-generic access.
    /// </summary>
    object PayloadAsObject { get; }

    /// <summary>
    /// Gets the collection of error messages, if any occurred during the operation.
    /// </summary>
    string[] Errors { get; }
}

/// <summary>
/// Represents a strongly-typed result of a Benzene operation with a typed payload.
/// </summary>
/// <typeparam name="T">The type of the payload.</typeparam>
public interface IBenzeneResult<T> : IBenzeneResult
{
    /// <summary>
    /// Gets the strongly-typed payload of the result.
    /// </summary>
    T Payload { get; }
}