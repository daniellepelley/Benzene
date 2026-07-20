using Benzene.Abstractions.Messages;

namespace Benzene.ResponseEvents;

/// <summary>
/// App-wide, queryable view of every response-event mapping registered by any pipeline's
/// <c>UseResponseEvents(...)</c> call, plus any declaration-only published events
/// (<c>AddResponseEventDeclarations(...)</c>) - resolve it from DI to see exactly what this
/// service publishes, as plain data.
/// </summary>
public interface IResponseEventCatalog
{
    /// <summary>Every registered mapping, across all pipelines.</summary>
    IReadOnlyList<IResponseEventMapping> Mappings { get; }

    /// <summary>Every declaration-only published-event definition.</summary>
    IReadOnlyList<IMessageDefinition> DeclaredDefinitions { get; }

    /// <summary>
    /// Whether any registered mapping could publish an event for a successful response on
    /// <paramref name="topic"/> (see <see cref="IResponseEventMapping.Covers"/>). Used by the
    /// unmapped-response-handler diagnostic.
    /// </summary>
    /// <param name="topic">The (source) topic to test coverage for.</param>
    /// <returns><c>true</c> if at least one mapping covers <paramref name="topic"/>.</returns>
    bool CoversTopic(ITopic topic);
}

/// <summary>
/// Default <see cref="IResponseEventCatalog"/>: aggregates the <see cref="ResponseEventMappings"/>
/// singleton instances each <c>UseResponseEvents(...)</c> call registered. Also an
/// <see cref="IMessageDefinitionFinder{TMessageDefinition}"/>, so mappings with a declared payload
/// type (<c>Map&lt;TPayload&gt;(...)</c>) flow into generated AsyncAPI / event-service specs as
/// published events - one declaration drives both runtime behavior and the spec.
/// </summary>
public sealed class ResponseEventCatalog : IResponseEventCatalog, IMessageDefinitionFinder<IMessageDefinition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventCatalog"/> class.
    /// </summary>
    /// <param name="pipelineMappings">Every pipeline's registered mapping set, injected as the collection of registered singletons.</param>
    /// <param name="declarations">Every registered declaration-only definition set.</param>
    public ResponseEventCatalog(IEnumerable<ResponseEventMappings> pipelineMappings, IEnumerable<ResponseEventDeclarations> declarations)
    {
        Mappings = pipelineMappings.SelectMany(x => x.Mappings).ToArray();
        DeclaredDefinitions = declarations.SelectMany(x => x.Definitions).ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<IResponseEventMapping> Mappings { get; }

    /// <inheritdoc />
    public IReadOnlyList<IMessageDefinition> DeclaredDefinitions { get; }

    /// <inheritdoc />
    public bool CoversTopic(ITopic topic) => Mappings.Any(mapping => mapping.Covers(topic));

    /// <summary>
    /// Returns a definition for every mapping that declared both an event topic and a payload type,
    /// plus every declaration-only definition. Convention rules (which derive topics at runtime)
    /// have no static definition and are skipped.
    /// </summary>
    /// <returns>The published-event definitions for spec generation.</returns>
    public IMessageDefinition[] FindDefinitions()
    {
        return Mappings
            .Where(x => x.EventTopic != null && x.PayloadType != null)
            .Select(IMessageDefinition (x) => new ResponseEventDefinition(x.EventTopic!, x.PayloadType!))
            .Concat(DeclaredDefinitions)
            .ToArray();
    }
}
