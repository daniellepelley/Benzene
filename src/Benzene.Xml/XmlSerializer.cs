using System.Collections.Concurrent;
using System.Xml;
using Benzene.Abstractions.Serialization;

namespace Benzene.Xml;

public class XmlSerializer : ISerializer
{
    // System.Xml.Serialization.XmlSerializer's own constructor already caches (and reuses) the
    // dynamically-generated serialization assembly per type internally, but still repeats its own
    // cache lookup/lock and object construction on every call; caching the instance here removes
    // that repeated overhead for a type once it's been serialized/deserialized once.
    private static readonly ConcurrentDictionary<Type, System.Xml.Serialization.XmlSerializer> SerializersByType = new();

    public string Serialize(Type type, object payload)
    {
        var xsSubmit = GetSerializer(type);

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
        return GetSerializer(type).Deserialize(xmlTextReader);
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

    private static System.Xml.Serialization.XmlSerializer GetSerializer(Type type)
    {
        return SerializersByType.GetOrAdd(type, static t => new System.Xml.Serialization.XmlSerializer(t));
    }
}
