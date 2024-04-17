using System.Text.Json;
using Benzene.Abstractions.Serialization;

namespace Benzene.Clients;

public class JsonSerializer : ISerializer 
{
    public string Serialize(Type type, object payload)
    {
        return Serialize(payload);
    }

    public string Serialize<T>(T payload)
    {
        return System.Text.Json.JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public object Deserialize(Type type, string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize(payload, type, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public T Deserialize<T>(string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
