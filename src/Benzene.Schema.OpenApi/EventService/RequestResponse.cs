using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;

namespace Benzene.Schema.OpenApi.EventService;

public class RequestResponse : IOpenApiSerializable
{
    public string Topic { get; set; }
    public string Version { get; set; }

    /// <summary>
    /// True when this is a reserved Benzene utility topic (spec, health, mesh, …) rather than a
    /// domain topic — see <see cref="ReservedTopics"/>. Serialized as <c>reserved</c> only when true,
    /// so spec/mesh tooling can group or hide utility topics separately from the service's domain.
    /// </summary>
    [JsonProperty("reserved")]
    public bool Reserved { get; set; }

    public HttpMapping[] HttpMappings { get; set; } = Array.Empty<HttpMapping>();
    public OpenApiSchema Request { get; set; }
    public OpenApiSchema Response { get; set; }

    /// <summary>
    /// An example request payload for this topic. Generated from the request schema during
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
        if (Reserved)
        {
            writer.WriteProperty("reserved", true);
        }
        writer.WriteOptionalCollection("httpMappings", HttpMappings, (w, o) => o.SerializeAsV3(w));
        writer.WriteRequiredObject("request", Request, (w, o) => o.SerializeAsV3(w));
        writer.WriteRequiredObject("response", Response, (w, o) => o.SerializeAsV3(w));

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
