using System;
using System.Dynamic;
using System.Text;
using Benzene.Abstractions.Results;
using Benzene.Core.Serialization;

namespace Benzene.Elements.Core
{
    public static class PayloadSerializer
    {
        public static string SerializePayload(object payload)
        {
            if (payload is IRawJsonMessage rawJsonMessage)
            {
                return CamelCaseJson(rawJsonMessage.Json);
            }

            if (payload is IRawStringMessage rawStringMessage)
            {
                return rawStringMessage.Content;
            }

            if (payload is IBase64JsonMessage base64JsonMessage)
            {
                if (string.IsNullOrEmpty(base64JsonMessage.Base64Json))
                {
                    return null;
                }

                try
                {
                    var data = Convert.FromBase64String(base64JsonMessage.Base64Json);
                    return CamelCaseJson(Encoding.UTF8.GetString(data));
                }
                catch
                {
                    return null;
                }
            }

            return SerializeObject(payload);
        }

        private static string SerializeObject(object payload)
        {
            return new JsonSerializer().Serialize(payload); 
        }

        private static string CamelCaseJson(string json)
        {
            var obj = new JsonSerializer().Deserialize<ExpandoObject>(json);
            return SerializeObject(obj);
        }
    }
}
