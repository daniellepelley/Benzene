using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Versioning.Response;

/// <summary>
/// A shim <see cref="IMessageHandlerResult"/> handed to the inner response payload mapper: same topic,
/// status, success and errors as the real result, but carrying the downcast payload and a
/// <see cref="ResponseTypeOverrideDefinition"/> so serialization uses the requested version's type.
/// Lets the response casting decorator reuse the inner mapper's serialization/raw-content handling
/// wholesale instead of reimplementing it (docs/specification/versioning.md §4.2).
/// </summary>
internal class CastMessageHandlerResult : IMessageHandlerResult
{
    public CastMessageHandlerResult(ITopic? topic, IMessageHandlerDefinition definition, IBenzeneResult original, object downcastPayload)
    {
        Topic = topic;
        MessageHandlerDefinition = definition;
        BenzeneResult = new CastBenzeneResult(original, downcastPayload);
    }

    public ITopic? Topic { get; }
    public IMessageHandlerDefinition? MessageHandlerDefinition { get; }
    public IBenzeneResult BenzeneResult { get; }

    private class CastBenzeneResult : IBenzeneResult
    {
        private readonly IBenzeneResult _original;

        public CastBenzeneResult(IBenzeneResult original, object payload)
        {
            _original = original;
            PayloadAsObject = payload;
        }

        public string Status => _original.Status;
        public bool IsSuccessful => _original.IsSuccessful;
        public string[] Errors => _original.Errors;
        public object PayloadAsObject { get; }
    }
}
