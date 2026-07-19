using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// An <see cref="IMessageDefinition"/> describing one event this service publishes as a mapped
/// handler response - the (event topic, payload type) pair surfaced to spec generation
/// (AsyncAPI / event-service documents) by <see cref="ResponseEventCatalog.FindDefinitions"/>.
/// </summary>
public sealed class ResponseEventDefinition : IMessageDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventDefinition"/> class.
    /// </summary>
    /// <param name="topic">The event topic id.</param>
    /// <param name="payloadType">The event payload type.</param>
    public ResponseEventDefinition(string topic, Type payloadType)
    {
        Topic = new Topic(topic);
        RequestType = payloadType;
    }

    /// <inheritdoc />
    public ITopic Topic { get; }

    /// <inheritdoc />
    public Type RequestType { get; }
}
