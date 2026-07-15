namespace Benzene.Abstractions.MessageHandlers.MediaFormats;

/// <summary>
/// Selects which registered <see cref="IMediaFormat{TContext}"/> to use for reading the current
/// message's request body and for writing its response, evaluating every registered format's
/// applicability exactly once per message (a scoped implementation is expected to memoize both
/// decisions, since the negotiation predicates - typically header lookups - are otherwise repeated by
/// every caller that needs to know the selected format).
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this negotiator applies to.</typeparam>
public interface IMediaFormatNegotiator<TContext>
{
    /// <summary>
    /// Selects the format to deserialize the request body with, from the first registered format whose
    /// <see cref="IMediaFormat{TContext}.CanRead"/> matches <paramref name="context"/>, falling back to
    /// the process default format (JSON) if none match.
    /// </summary>
    /// <param name="context">The transport-specific context for the current message.</param>
    IMediaFormat<TContext> SelectRead(TContext context);

    /// <summary>
    /// Selects the format to serialize the response with, from the first registered format whose
    /// <see cref="IMediaFormat{TContext}.CanWrite"/> matches <paramref name="context"/> (typically an
    /// <c>accept</c> header match), falling back to <see cref="SelectRead"/>'s format if none match.
    /// </summary>
    /// <param name="context">The transport-specific context for the current message.</param>
    IMediaFormat<TContext> SelectWrite(TContext context);
}
