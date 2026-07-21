using System.Text.Json;
using Benzene.Abstractions.Serialization;

namespace Benzene.Clients;

public class JsonSerializer : ISerializer
{
    // Created once and shared: System.Text.Json caches its reflection-built type metadata per
    // JsonSerializerOptions instance, so per-call options re-paid that whole build on every
    // serialize/deserialize.
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string Serialize(Type type, object payload)
    {
        return Serialize(payload);
    }

    public string Serialize<T>(T payload)
    {
        return System.Text.Json.JsonSerializer.Serialize(payload, SerializeOptions);
    }

    public object Deserialize(Type type, string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize(payload, type, DeserializeOptions);
    }

    public T Deserialize<T>(string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(payload, DeserializeOptions);
    }
}
