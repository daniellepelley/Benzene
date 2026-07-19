using Benzene.Abstractions.Messages;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// A set of declaration-only published-event definitions - events this service sends (typically
/// directly via <c>IBenzeneMessageSender</c> from handler code) that should appear in generated
/// specs and the <see cref="IResponseEventCatalog"/> without any runtime republishing behavior.
/// Registered via <see cref="ResponseEventsExtensions.AddResponseEventDeclarations"/>; aggregated
/// by <see cref="ResponseEventCatalog"/> alongside the mapped response events.
/// </summary>
public sealed class ResponseEventDeclarations
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventDeclarations"/> class.
    /// </summary>
    /// <param name="definitions">The declared published-event definitions.</param>
    public ResponseEventDeclarations(IReadOnlyList<IMessageDefinition> definitions)
    {
        Definitions = definitions;
    }

    /// <summary>The declared published-event definitions.</summary>
    public IReadOnlyList<IMessageDefinition> Definitions { get; }
}
