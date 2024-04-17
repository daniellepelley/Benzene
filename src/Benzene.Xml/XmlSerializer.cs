using System.Xml;
using Benzene.Abstractions.Serialization;

namespace Benzene.Xml;

public class XmlSerializer : ISerializer
{
    public string Serialize(Type type, object payload)
    {
        var xsSubmit = new System.Xml.Serialization.XmlSerializer(type);

        using var sww = new StringWriter();
        using var writer = XmlWriter.Create(sww);
        xsSubmit.Serialize(writer, payload);
        return sww.ToString();
    }

    public string Serialize<T>(T payload)
    {
        return payload == null
            ? string.Empty
            : Serialize(typeof(T), payload);
    }

    public object? Deserialize(Type type, string payload)
    {
        using var stringReader = new StringReader(payload);
        using var xmlTextReader = new XmlTextReader(stringReader);
        return new System.Xml.Serialization.XmlSerializer(type).Deserialize(xmlTextReader);
    }

    public T? Deserialize<T>(string payload)
    {
        var obj = Deserialize(typeof(T), payload);

        if (obj == null)
        {
            return default;
        }

        return (T)obj;
    }
}