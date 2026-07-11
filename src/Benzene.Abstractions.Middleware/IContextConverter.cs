namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Provides bidirectional conversion between different context types in a middleware pipeline.
/// </summary>
/// <typeparam name="TContextIn">The input context type (typically from an outer pipeline stage).</typeparam>
/// <typeparam name="TContextOut">The output context type (typically for an inner pipeline stage).</typeparam>
/// <remarks>
/// Context converters enable splitting middleware pipelines into multiple stages with different context types.
/// This is useful for:
/// - Separating transport-specific concerns from business logic
/// - Creating reusable inner pipelines that operate on generic contexts
/// - Implementing hexagonal architecture where outer adapters convert to domain-specific contexts
/// - Handling protocol translation between different layers
/// The converter supports both forward transformation (creating the output context) and backward mapping (updating the input context with results).
/// </remarks>
public interface IContextConverter<TContextIn, TContextOut>
{
    /// <summary>
    /// Creates the output context asynchronously from the input context.
    /// </summary>
    /// <param name="contextIn">The input context to convert.</param>
    /// <returns>A task that represents the asynchronous operation and contains the created output context.</returns>
    /// <remarks>
    /// This method performs the forward transformation, typically:
    /// - Extracting relevant data from the input context
    /// - Creating a new output context instance
    /// - Mapping properties from input to output
    /// - Initializing output-specific state
    /// This is called before the inner pipeline executes.
    /// </remarks>
    public Task<TContextOut> CreateRequestAsync(TContextIn contextIn);

    /// <summary>
    /// Maps results from the output context back to the input context asynchronously.
    /// </summary>
    /// <param name="contextIn">The input context to update with results.</param>
    /// <param name="contextOut">The output context containing results from the inner pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method performs the backward mapping, typically:
    /// - Extracting results from the output context
    /// - Updating the input context with those results
    /// - Translating output-specific state back to input format
    /// - Handling status codes, errors, or other metadata
    /// This is called after the inner pipeline completes.
    /// </remarks>
    public Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut);
}