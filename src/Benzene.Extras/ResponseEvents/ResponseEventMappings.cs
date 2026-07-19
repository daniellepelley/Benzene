using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// One pipeline's immutable set of response-event mappings plus its publish-failure policy. Built by
/// <see cref="ResponseEventsBuilder"/>, held by that pipeline's
/// <see cref="ResponseEventsMiddlewareBuilder"/>, and also registered as a DI singleton instance so
/// <see cref="ResponseEventCatalog"/> can aggregate every pipeline's mappings for introspection.
/// </summary>
public sealed class ResponseEventMappings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventMappings"/> class.
    /// </summary>
    /// <param name="mappings">The mappings, evaluated in registration order.</param>
    /// <param name="publishFailureMode">What to do when publishing a matched event fails.</param>
    public ResponseEventMappings(IReadOnlyList<IResponseEventMapping> mappings, PublishFailureMode publishFailureMode)
    {
        Mappings = mappings;
        PublishFailureMode = publishFailureMode;
    }

    /// <summary>The mappings, in registration order.</summary>
    public IReadOnlyList<IResponseEventMapping> Mappings { get; }

    /// <summary>What to do when publishing a matched event fails.</summary>
    public PublishFailureMode PublishFailureMode { get; }

    /// <summary>
    /// Resolves every mapping that fires for the given handled message, in registration order.
    /// Multiple matches are allowed - each one publishes (fan-out).
    /// </summary>
    /// <param name="sourceTopic">The topic the message was routed on.</param>
    /// <param name="result">The handler's result.</param>
    /// <returns>The publications to send; empty when nothing matched.</returns>
    public IReadOnlyList<ResponseEventPublication> Resolve(ITopic sourceTopic, IBenzeneResult result)
    {
        List<ResponseEventPublication>? publications = null;
        foreach (var mapping in Mappings)
        {
            var publication = mapping.Resolve(sourceTopic, result);
            if (publication != null)
            {
                (publications ??= new List<ResponseEventPublication>()).Add(publication);
            }
        }

        return publications ?? (IReadOnlyList<ResponseEventPublication>)Array.Empty<ResponseEventPublication>();
    }
}
