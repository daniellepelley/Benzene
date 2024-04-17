using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace Benzene.Schema.OpenApi.EventService;

public class RequestResponse : IOpenApiSerializable
{
    public string Topic { get; set; }
    public string Version { get; set; }
    public HttpMapping[] HttpMappings { get; set; } = Array.Empty<HttpMapping>();
    public OpenApiSchema Request { get; set; }
    public OpenApiSchema Response { get; set; }

    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteProperty("topic", Topic);
        writer.WriteOptionalCollection("httpMappings", HttpMappings, (w, o) => o.SerializeAsV3(w));
        writer.WriteRequiredObject("request", Request, (w, o) => o.SerializeAsV3(w));
        writer.WriteRequiredObject("response", Response, (w, o) => o.SerializeAsV3(w));

        writer.WriteEndObject();
    }

    public void SerializeAsV2(IOpenApiWriter writer)
    {
        SerializeAsV3(writer);
    }
}
