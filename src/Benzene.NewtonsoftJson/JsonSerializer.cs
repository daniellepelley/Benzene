using System;
using Benzene.Abstractions.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Benzene.NewtonsoftJson;

public class JsonSerializer : ISerializer
{
    public virtual string Serialize(Type type, object payload)
    {
        return Serialize(payload);
    }

    public virtual string Serialize<T>(T payload)
    {
        return JsonConvert.SerializeObject(payload, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
    }

    public virtual object Deserialize(Type type, string payload)
    {
        return JsonConvert.DeserializeObject(payload, type);
    }

    public virtual T Deserialize<T>(string payload)
    {
        return JsonConvert.DeserializeObject<T>(payload, new JsonSerializerSettings());
    }
}
