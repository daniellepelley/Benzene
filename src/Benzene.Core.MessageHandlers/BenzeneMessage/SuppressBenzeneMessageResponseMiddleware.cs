using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// Marks the current message's <see cref="BenzeneMessageResponseSuppression"/> so
/// <see cref="BenzeneMessageHandlerResultSetter"/> skips serializing a response that a one-way host
/// would discard. Added to the front of a BenzeneMessage pipeline via <c>SuppressResponse()</c>;
/// must run before the terminal message router so the flag is set before the setter reads it.
/// </summary>
public class SuppressBenzeneMessageResponseMiddleware : IMiddleware<BenzeneMessageContext>
{
    private readonly BenzeneMessageResponseSuppression _suppression;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuppressBenzeneMessageResponseMiddleware"/> class.
    /// </summary>
    /// <param name="suppression">The current message's scoped suppression flag.</param>
    public SuppressBenzeneMessageResponseMiddleware(BenzeneMessageResponseSuppression suppression)
    {
        _suppression = suppression;
    }

    /// <inheritdoc />
    public string Name => "SuppressBenzeneMessageResponse";

    /// <summary>Sets the suppression flag, then continues the pipeline.</summary>
    /// <param name="context">The current message context (untouched).</param>
    /// <param name="next">The rest of the pipeline.</param>
    public Task HandleAsync(BenzeneMessageContext context, Func<Task> next)
    {
        _suppression.IsSuppressed = true;
        return next();
    }
}
