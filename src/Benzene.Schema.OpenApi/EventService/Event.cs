using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace Benzene.Schema.OpenApi.EventService;

public class Event : IOpenApiSerializable
{
    public Event(string topic, OpenApiSchema message)
    {
        Topic = topic;
        Message = message;
    }

    public string Topic { get; }
    public OpenApiSchema Message { get; }

    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteProperty("topic", Topic);
        writer.WriteRequiredObject("message", Message, (w, o) => o.SerializeAsV3(w));

        writer.WriteEndObject();
    }

    public void SerializeAsV2(IOpenApiWriter writer)
    {
        SerializeAsV3(writer);
    }
}
