using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Benzene.Core.Serialization.JsonSerializer;

namespace Benzene.Elements.Core.Patches;

public class PatchJsonSerializer : JsonSerializer
{
    public override object Deserialize(Type type, string payload)
    {
        var output = base.Deserialize(type, payload);

        if (typeof(IPatchMessage).IsAssignableFrom(type))
        {
            var patchMessage = (IPatchMessage)output!;

            if (patchMessage.UpdatedFields.Any())
            {
                return output;
            }

            var j = JObject.Parse(payload);

            foreach (var key in j.Properties().Select(p => p.Name.ToLowerInvariant()))
            {
                patchMessage.UpdatedFields.Add(key);
            }
        }

        return output;
    }

    public override T Deserialize<T>(string payload)
    {
        return (T)Deserialize(typeof(T), payload);
    }

    public override string Serialize<T>(T payload)
    {
        if (typeof(IPatchMessage).IsAssignableFrom(typeof(T)))
        {
            var patchMessage = (IPatchMessage)payload!;

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new PatchContractResolver(patchMessage.UpdatedFields.ToArray())
            };
            return JsonConvert.SerializeObject(patchMessage, settings);
        }

        return base.Serialize(payload);
    }
}
