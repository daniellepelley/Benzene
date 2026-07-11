using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides an inline context converter using functions for transformation and response mapping.
/// </summary>
/// <typeparam name="TContextIn">The input context type to convert from.</typeparam>
/// <typeparam name="TContextOut">The output context type to convert to.</typeparam>
/// <remarks>
/// This class enables context conversion without creating a dedicated converter class,
/// simplifying scenarios where conversion logic is straightforward and can be expressed as functions.
/// </remarks>
public class InlineContextConverter<TContextIn, TContextOut>(
    Func<TContextIn, TContextOut> createContextFunc,
    Action<TContextIn, TContextOut> mapContext)
    : IContextConverter<TContextIn, TContextOut>
{
    /// <summary>
    /// Creates the output context from the input context.
    /// </summary>
    /// <param name="contextIn">The input context to convert.</param>
    /// <returns>A task that represents the asynchronous operation, containing the created output context.</returns>
    public Task<TContextOut> CreateRequestAsync(TContextIn contextIn)
        => Task.FromResult(createContextFunc(contextIn));

    /// <summary>
    /// Maps the response from the output context back to the input context.
    /// </summary>
    /// <param name="contextIn">The input context to map the response to.</param>
    /// <param name="contextOut">The output context containing the response.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut)
    {
        mapContext(contextIn, contextOut);
        return Task.CompletedTask;
    }
}