using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;

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

    /// <summary>
    /// An example message payload for this event. Generated from the message schema during
    /// <see cref="EventServiceDocumentBuilder.Build"/> unless one was supplied. Ignored by
    /// Newtonsoft deserialization (an <see cref="IOpenApiAny"/> can't be materialized from JSON
    /// directly); <see cref="EventServiceDocumentDeserializer"/> reads it back explicitly.
    /// </summary>
    [JsonIgnore]
    public IOpenApiAny? Example { get; set; }

    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteProperty("topic", Topic);
        writer.WriteRequiredObject("message", Message, (w, o) => o.SerializeAsV3(w));

        if (Example != null)
        {
            writer.WritePropertyName("example");
            writer.WriteAny(Example);
        }

        writer.WriteEndObject();
    }

    public void SerializeAsV2(IOpenApiWriter writer)
    {
        SerializeAsV3(writer);
    }
}
