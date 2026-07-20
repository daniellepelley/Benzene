using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.ResponseEvents;

/// <summary>
/// Opt-in startup diagnostic for the "response payload with nowhere to go" gap: a
/// request/response handler (<see cref="IMessageHandler{TRequest,TResponse}"/>) whose response is
/// silently discarded when it runs on a fire-and-forget transport and no <c>UseResponseEvents</c>
/// mapping republishes it. Mirrors <c>ValidateOutboundRouting</c> - call once after wiring, decide
/// what to do with the findings.
/// </summary>
/// <remarks>
/// Because Benzene handlers are transport-agnostic (the same handler can run over HTTP <em>and</em>
/// SQS), this cannot know from registration alone whether a given handler's response is meant to be
/// delivered. It reports every response-returning handler whose topic no mapping covers; a handler
/// served only over HTTP/gRPC (where the response <em>is</em> the reply) is a false positive to
/// ignore. It is therefore advisory - it never throws.
/// </remarks>
public static class ResponseEventDiagnosticsExtensions
{
    /// <summary>
    /// Finds every registered handler that returns a response payload on a topic no response-event
    /// mapping covers. Returns an empty array when handler discovery isn't available.
    /// </summary>
    /// <param name="serviceResolver">Resolves <see cref="IMessageHandlersFinder"/> and (optionally) <see cref="IResponseEventCatalog"/>.</param>
    /// <returns>The gaps, ordered by topic then handler name; empty when there are none.</returns>
    public static ResponseEventGap[] FindUnmappedResponseHandlers(this IServiceResolver serviceResolver)
    {
        var handlersFinder = serviceResolver.TryGetService<IMessageHandlersFinder>();
        if (handlersFinder == null)
        {
            return Array.Empty<ResponseEventGap>();
        }

        var catalog = serviceResolver.TryGetService<IResponseEventCatalog>();

        return handlersFinder.FindDefinitions()
            .Where(definition => definition.ResponseType != typeof(Void)
                                 && definition.HandlerType != typeof(Void)
                                 && !(catalog?.CoversTopic(definition.Topic) ?? false))
            .OrderBy(definition => definition.Topic.Id, StringComparer.Ordinal)
            .ThenBy(definition => definition.HandlerType.Name, StringComparer.Ordinal)
            .Select(definition => new ResponseEventGap(definition.Topic, definition.HandlerType, definition.ResponseType))
            .ToArray();
    }

    /// <summary>
    /// Runs <see cref="FindUnmappedResponseHandlers"/> and logs each gap as a warning, returning the
    /// findings so the caller can act further (e.g. fail a startup check in CI). A no-op with no
    /// findings.
    /// </summary>
    /// <param name="serviceResolver">Resolves the handler finder, response-event catalog, and (if <paramref name="logger"/> is null) an <see cref="ILogger"/>.</param>
    /// <param name="logger">Logger to warn on; resolved from DI when null.</param>
    /// <returns>The gaps found (also logged).</returns>
    public static ResponseEventGap[] LogUnmappedResponseHandlers(this IServiceResolver serviceResolver, ILogger? logger = null)
    {
        var gaps = serviceResolver.FindUnmappedResponseHandlers();
        if (gaps.Length == 0)
        {
            return gaps;
        }

        logger ??= serviceResolver.TryGetService<ILoggerFactory>()?.CreateLogger(typeof(ResponseEventDiagnosticsExtensions).FullName!);
        if (logger != null)
        {
            foreach (var gap in gaps)
            {
                logger.LogWarning("{ResponseEventGap}", gap.Description);
            }
        }

        return gaps;
    }
}
