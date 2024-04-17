using System;
using System.Text.Json;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Serialization;

public class JsonSerializer : ISerializer
{
    public virtual string Serialize(Type type, object payload)
    {
        return Serialize(payload);
    }

    public virtual string Serialize<T>(T payload)
    {
        return System.Text.Json.JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public virtual object Deserialize(Type type, string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize(payload, type, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public virtual T Deserialize<T>(string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
