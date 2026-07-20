using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.ResponseEvents;

/// <summary>
/// One rule for turning a handler's response into a follow-up event. Registered per pipeline via
/// <see cref="ResponseEventsBuilder"/> and evaluated by
/// <see cref="ResponseEventsMiddleware{TRequest,TResponse}"/> after the handler runs; every mapping
/// that resolves a publication publishes (fan-out is allowed).
/// The metadata properties exist so mappings are introspectable as plain data - via
/// <see cref="IResponseEventCatalog"/> at runtime and spec generation
/// (<c>IMessageDefinitionFinder</c>) at build time. Convention-based rules that derive topics at
/// runtime return <c>null</c> metadata; explicit mappings should populate it.
/// </summary>
public interface IResponseEventMapping
{
    /// <summary>Human-readable summary of the rule, for diagnostics and introspection.</summary>
    string Description { get; }

    /// <summary>The source topic id this mapping listens on, or <c>null</c> for convention rules that match dynamically.</summary>
    string? SourceTopic { get; }

    /// <summary>The event topic id this mapping publishes on, or <c>null</c> for convention rules that derive it at runtime.</summary>
    string? EventTopic { get; }

    /// <summary>The declared event payload type (used for spec generation), or <c>null</c> when undeclared.</summary>
    Type? PayloadType { get; }

    /// <summary>
    /// Decides whether this mapping fires for the given handled message, and with what.
    /// </summary>
    /// <param name="sourceTopic">The topic the message was routed on.</param>
    /// <param name="result">The handler's result (status, success flag, payload).</param>
    /// <returns>The event to publish, or <c>null</c> if this mapping does not apply.</returns>
    ResponseEventPublication? Resolve(ITopic sourceTopic, IBenzeneResult result);
}
