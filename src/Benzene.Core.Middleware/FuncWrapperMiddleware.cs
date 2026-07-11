using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides middleware that wraps a function, enabling inline middleware definition without creating a separate class.
/// </summary>
/// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
/// <remarks>
/// This class enables the fluent API's Use() methods to accept functions directly, simplifying
/// middleware creation for simple scenarios that don't require a dedicated middleware class.
/// </remarks>
public class FuncWrapperMiddleware<TContext>(string name, Func<TContext, Func<Task>, Task> func) : IMiddleware<TContext>
{
    /// <summary>
    /// Gets the name of this middleware component.
    /// </summary>
    public string Name { get; } = !string.IsNullOrEmpty(name) ? name : Constants.Unnamed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FuncWrapperMiddleware{TContext}"/> class with an unnamed middleware function.
    /// </summary>
    /// <param name="func">The function that defines the middleware behavior.</param>
    public FuncWrapperMiddleware(Func<TContext, Func<Task>, Task> func)
        :this(Constants.Unnamed, func)
    { }

    /// <summary>
    /// Handles the middleware execution by invoking the wrapped function.
    /// </summary>
    /// <param name="context">The current context being processed.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task HandleAsync(TContext context, Func<Task> next) => func(context, next);
}
