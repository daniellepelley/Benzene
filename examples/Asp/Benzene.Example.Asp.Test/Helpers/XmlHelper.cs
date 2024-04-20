using System.Xml;
using System.Xml.Serialization;

namespace Benzene.Example.Asp.Test.Helpers;

public static class XmlHelper
{
    public static string ToXml<T>(T message)
    {
        var xmlSerializer = new XmlSerializer(typeof(T));

        using var sww = new StringWriter();
        using var writer = XmlWriter.Create(sww);
        xmlSerializer.Serialize(writer, message);
        return sww.ToString();
    }
}