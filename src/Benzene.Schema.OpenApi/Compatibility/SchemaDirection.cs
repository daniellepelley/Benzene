namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// Which side of a message a change affects. Compatibility is asymmetric: the same structural change
/// is safe on one side and breaking on the other, because the client <em>produces</em> requests and
/// <em>consumes</em> responses.
/// </summary>
public enum SchemaDirection
{
    /// <summary>The request type — produced by the client, consumed by the service.</summary>
    Request,

    /// <summary>The response type — produced by the service, consumed by the client.</summary>
    Response,

    /// <summary>An event/broadcast message payload.</summary>
    Event
}
