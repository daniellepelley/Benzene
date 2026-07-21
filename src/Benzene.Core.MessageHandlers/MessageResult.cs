using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Supplies the default status codes used by the message handler pipeline (e.g. <see cref="MessageHandler{TRequest,TResponse}"/>,
/// <see cref="MessageRouter{TContext}"/>) when it needs to report validation failures, missing
/// resources, or malformed requests, without hard-coding transport-specific status codes.
/// </summary>
/// <remarks>
/// The default implementation, <see cref="DefaultStatuses"/>, maps these to <see cref="Benzene.Results.BenzeneResultStatus"/>
/// values. Register a different implementation to customize the statuses used across the pipeline.
/// </remarks>
public interface IDefaultStatuses
{
    /// <summary>
    /// Gets the status used when a message handler rejects a request with an <see cref="ArgumentException"/>.
    /// </summary>
    public string ValidationError { get; }

    /// <summary>
    /// Gets the status used when no handler or handler definition could be found for a topic.
    /// </summary>
    public string NotFound { get; }

    /// <summary>
    /// Gets the status used when an inbound message could not be deserialized/mapped into the
    /// handler's request type.
    /// </summary>
    public string BadRequest { get; }
}
