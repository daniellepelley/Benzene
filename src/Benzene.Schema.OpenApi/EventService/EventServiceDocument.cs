using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace Benzene.Schema.OpenApi.EventService;

public class EventServiceDocument : IOpenApiSerializable
{
    public EventServiceDocument(OpenApiInfo info, OpenApiTag[] tags, RequestResponse[] requests, Event[] events, OpenApiComponents components)
    {
        Info = info;
        Tags = tags;
        Requests = requests;
        Events = events;
        Components = components;
    }

    public OpenApiInfo Info { get; set; }
    public OpenApiTag[] Tags { get; set; }
    public RequestResponse[] Requests { get; set; }
    public Event[] Events { get; set; }
    public OpenApiComponents Components { get; set; }

    /// <summary>
    /// The path of the service's BenzeneMessage-over-HTTP endpoint (see
    /// <c>Benzene.Http.BenzeneMessage</c>'s <c>UseBenzeneMessage</c>), or null when the service
    /// doesn't expose one. Serialized as the optional top-level <c>messageEndpoint</c> field —
    /// spec consumers feature-detect send capability on it.
    /// </summary>
    public string? MessageEndpoint { get; set; }

    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteProperty(OpenApiConstants.OpenApi, "3.0.1");
        writer.WriteRequiredObject(OpenApiConstants.Info, Info, (w, i) => i.SerializeAsV3(w));
        writer.WriteOptionalCollection(OpenApiConstants.Tags, Tags, (w, t) => t.SerializeAsV3WithoutReference(w));
        writer.WriteProperty("messageEndpoint", MessageEndpoint);
        writer.WriteRequiredCollection("requests", Requests, (w, c) => c.SerializeAsV3(w));
        writer.WriteRequiredCollection("events", Events, (w, c) => c.SerializeAsV3(w));
        writer.WriteRequiredObject(OpenApiConstants.Components, Components, (w, c) => c.SerializeAsV3(w));
        writer.WriteEndObject();
    }

    public void SerializeAsV2(IOpenApiWriter writer)
    {
        SerializeAsV3(writer);
    }
}
