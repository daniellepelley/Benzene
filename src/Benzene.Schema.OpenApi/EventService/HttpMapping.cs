using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Benzene.Schema.OpenApi.EventService;

public class HttpMapping : IOpenApiSerializable
{
    public string Method { get; set; }
    public string Path { get; set; }
    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteProperty("method", Method);
        writer.WriteProperty("path", Path);
        writer.WriteEndObject();
    }

    public void SerializeAsV2(IOpenApiWriter writer)
    {
        SerializeAsV3(writer);
    }
}
