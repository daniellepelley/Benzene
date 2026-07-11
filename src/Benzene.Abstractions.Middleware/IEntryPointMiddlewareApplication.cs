namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Marker interface for entry point middleware applications.
/// </summary>
/// <remarks>
/// This interface serves as a base marker for all entry point middleware applications in Benzene.
/// Entry points are the top-level bootstrap components that receive external events (HTTP requests, queue messages, etc.)
/// and process them through a middleware pipeline. This marker enables registration, discovery, and dependency injection scenarios.
/// </remarks>
public interface IEntryPointMiddlewareApplication;

/// <summary>
/// Represents an entry point middleware application that processes events without returning a result.
/// </summary>
/// <typeparam name="TEvent">The type of event to process. Uses contravariance to allow flexible event handling.</typeparam>
/// <remarks>
/// This interface is typically implemented by transport adapters (e.g., message queue listeners, background job processors)
/// that receive events from external sources and process them through a middleware pipeline without requiring a response.
/// Common use cases include:
/// - Message queue consumers
/// - Event-driven architectures
/// - Fire-and-forget operations
/// - Background job processing
/// </remarks>
public interface IEntryPointMiddlewareApplication<in TEvent> : IEntryPointMiddlewareApplication
{
    /// <summary>
    /// Sends an event through the middleware pipeline for processing.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method initiates the middleware pipeline execution with the provided event.
    /// The pipeline will execute all registered middleware in order until completion or until a middleware short-circuits the chain.
    /// </remarks>
    Task SendAsync(TEvent @event);
}

/// <summary>
/// Represents an entry point middleware application that processes events and returns a result.
/// </summary>
/// <typeparam name="TEvent">The type of event to process. Uses contravariance to allow flexible event handling.</typeparam>
/// <typeparam name="TResult">The type of result returned after processing the event.</typeparam>
/// <remarks>
/// This interface is typically implemented by transport adapters (e.g., HTTP handlers, RPC endpoints)
/// that receive requests from external sources, process them through a middleware pipeline, and return a response.
/// Common use cases include:
/// - HTTP request/response processing
/// - RPC method invocations
/// - Query handlers
/// - Request/reply messaging patterns
/// </remarks>
public interface IEntryPointMiddlewareApplication<in TEvent, TResult> : IEntryPointMiddlewareApplication
{
    /// <summary>
    /// Sends an event through the middleware pipeline for processing and returns a result.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <returns>A task that represents the asynchronous operation and contains the processing result.</returns>
    /// <remarks>
    /// This method initiates the middleware pipeline execution with the provided event.
    /// The pipeline will execute all registered middleware in order until completion or until a middleware short-circuits the chain.
    /// The result is typically populated by middleware components or handlers within the pipeline.
    /// </remarks>
    Task<TResult> SendAsync(TEvent @event);
}
