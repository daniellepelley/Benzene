using System.Text.Json;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Serialization;

public class JsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonSerializer()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public JsonSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public virtual string Serialize(Type type, object payload)
    {
        return Serialize(payload);
    }

    public virtual string Serialize<T>(T payload)
    {
        return System.Text.Json.JsonSerializer.Serialize(payload, _jsonSerializerOptions);
    }

    public virtual object Deserialize(Type type, string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize(payload, type, _jsonSerializerOptions);
    }

    public virtual T Deserialize<T>(string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions);
    }
}
