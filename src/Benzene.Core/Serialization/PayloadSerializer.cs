using System;
using System.Dynamic;
using System.Text;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Serialization;

public class PayloadSerializer : ISerializer
{
    private readonly ISerializer _serializer;

    public PayloadSerializer(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public static string? SerializePayload(object payload)
    {
        switch (payload)
        {
            case IRawJsonMessage rawJsonMessage:
                return CamelCaseJson(rawJsonMessage.Json);
            case IRawStringMessage rawStringMessage:
                return rawStringMessage.Content;
            case IBase64JsonMessage base64JsonMessage when string.IsNullOrEmpty(base64JsonMessage.Base64Json):
                return null;
            case IBase64JsonMessage base64JsonMessage:
                try
                {
                    var data = Convert.FromBase64String(base64JsonMessage.Base64Json);
                    return CamelCaseJson(Encoding.UTF8.GetString(data));
                }
                catch
                {
                    return null;
                }

            default:
                return null;
        }
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

    public string Serialize(Type type, object payload)
    {
        var value = SerializePayload(payload);
        return value ?? _serializer.Serialize(type, payload);
    }

    public string Serialize<T>(T payload)
    {
        if (payload == null)
        {
            return _serializer.Serialize(payload);
        }

        var value = SerializePayload(payload);
        return value ?? _serializer.Serialize(payload);
    }

    public object? Deserialize(Type type, string payload)
    {
        return _serializer.Deserialize(type, payload);
    }

    public T? Deserialize<T>(string payload)
    {
        return _serializer.Deserialize<T>(payload);
    }
}
