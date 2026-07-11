using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Represents a middleware application that processes events without returning a result.
/// </summary>
/// <typeparam name="TEvent">The type of event to process.</typeparam>
/// <remarks>
/// This interface defines the contract for event-driven middleware applications that execute a middleware pipeline
/// without producing a return value. Common scenarios include:
/// - Message queue processing
/// - Event broadcasting
/// - Fire-and-forget operations
/// - Background task execution
/// The application is responsible for creating the appropriate context, executing the middleware pipeline,
/// and managing the service resolver lifecycle.
/// </remarks>
public interface IMiddlewareApplication<TEvent>
{
    /// <summary>
    /// Handles the event by executing it through a middleware pipeline.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="serviceResolverFactory">The factory for creating scoped service resolvers for dependency injection within the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// Implementations typically:
    /// - Create a scoped service resolver from the factory
    /// - Transform the event into an appropriate context object
    /// - Execute the middleware pipeline with the context
    /// - Ensure proper disposal of the service resolver scope
    /// </remarks>
    Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory);
}

/// <summary>
/// Represents a middleware application that processes requests and returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of request to process.</typeparam>
/// <typeparam name="TResponse">The type of response to return.</typeparam>
/// <remarks>
/// This interface defines the contract for request/response middleware applications that execute a middleware pipeline
/// and produce a result. Common scenarios include:
/// - HTTP request handling
/// - RPC method invocations
/// - Query processing
/// - Request/reply messaging patterns
/// The application is responsible for creating the appropriate context, executing the middleware pipeline,
/// extracting the response from the context, and managing the service resolver lifecycle.
/// </remarks>
public interface IMiddlewareApplication<TRequest, TResponse>
{
    /// <summary>
    /// Handles the request by executing it through a middleware pipeline and returns a response.
    /// </summary>
    /// <param name="event">The request to process.</param>
    /// <param name="serviceResolverFactory">The factory for creating scoped service resolvers for dependency injection within the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation and contains the response.</returns>
    /// <remarks>
    /// Implementations typically:
    /// - Create a scoped service resolver from the factory
    /// - Transform the request into an appropriate context object
    /// - Execute the middleware pipeline with the context
    /// - Extract the response from the context after pipeline execution
    /// - Ensure proper disposal of the service resolver scope
    /// </remarks>
    Task<TResponse> HandleAsync(TRequest @event, IServiceResolverFactory serviceResolverFactory);
}