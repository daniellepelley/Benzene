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

    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteProperty(OpenApiConstants.OpenApi, "3.0.1");
        writer.WriteRequiredObject(OpenApiConstants.Info, Info, (w, i) => i.SerializeAsV3(w));
        writer.WriteOptionalCollection(OpenApiConstants.Tags, Tags, (w, t) => t.SerializeAsV3WithoutReference(w));
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
