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
            // CamelCasePropertyNamesContractResolver's default naming strategy has
            // ProcessDictionaryKeys = true, so it camel-cases DICTIONARY KEYS too (not just property
            // names) - corrupting free-form keys ("MyKey" -> "myKey"), and deserialize doesn't undo
            // it, so a round-trip silently loses the original keys. The default System.Text.Json
            // serializer does not rename dictionary keys; match it by camel-casing property names only.
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    OverrideSpecifiedNames = true
                }
            }
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
