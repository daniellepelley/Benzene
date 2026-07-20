using System.Collections.Concurrent;
using System.Text;
using System.Xml;
using Benzene.Abstractions.Serialization;

namespace Benzene.Xml;

public class XmlSerializer : ISerializer
{
    // A plain StringWriter always reports Encoding.Unicode (UTF-16), so XmlWriter stamps
    // `encoding="utf-16"` into the XML declaration. The body is returned as a string and then
    // transmitted as UTF-8 (like every other body), so that declaration contradicts the bytes on the
    // wire and a conformant XML client that honors it fails to parse the response. Report UTF-8 so the
    // declaration matches the wire.
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    // System.Xml.Serialization.XmlSerializer's own constructor already caches (and reuses) the
    // dynamically-generated serialization assembly per type internally, but still repeats its own
    // cache lookup/lock and object construction on every call; caching the instance here removes
    // that repeated overhead for a type once it's been serialized/deserialized once.
    private static readonly ConcurrentDictionary<Type, System.Xml.Serialization.XmlSerializer> SerializersByType = new();

    public string Serialize(Type type, object payload)
    {
        var xsSubmit = GetSerializer(type);

        using var sww = new Utf8StringWriter();
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
