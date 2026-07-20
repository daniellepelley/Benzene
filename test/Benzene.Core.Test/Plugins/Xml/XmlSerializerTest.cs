using Benzene.Test.Examples;
using Benzene.Xml;
using Xunit;

namespace Benzene.Test.Plugins.Xml;

public class XmlSerializerTest
{
    [Fact]
    public void Serialization_RoundTrips()
    {
        var payload = new ExampleRequestPayload { Id = 42, Name = "foo" };

        var serializer = new XmlSerializer();

        var xml = serializer.Serialize(payload);
        var result = serializer.Deserialize<ExampleRequestPayload>(xml);

        Assert.Equal(payload.Id, result.Id);
        Assert.Equal(payload.Name, result.Name);
    }

    [Fact]
    public void Serialization_RepeatedCallsForSameType_ProduceIndependentCorrectResults()
    {
        // Exercises the cached System.Xml.Serialization.XmlSerializer instance across repeated
        // calls for the same CLR type, on both the same and a fresh XmlSerializer instance (the
        // cache is static, shared across instances).
        var serializer = new XmlSerializer();

        var first = serializer.Serialize(new ExampleRequestPayload { Id = 1, Name = "alice" });
        var second = new XmlSerializer().Serialize(new ExampleRequestPayload { Id = 2, Name = "bob" });

        var firstResult = serializer.Deserialize<ExampleRequestPayload>(first);
        var secondResult = new XmlSerializer().Deserialize<ExampleRequestPayload>(second);

        Assert.Equal(1, firstResult.Id);
        Assert.Equal("alice", firstResult.Name);
        Assert.Equal(2, secondResult.Id);
        Assert.Equal("bob", secondResult.Name);
    }

    [Fact]
    public void Serialize_NullPayload_ReturnsEmptyString()
    {
        var serializer = new XmlSerializer();

        Assert.Equal(string.Empty, serializer.Serialize<ExampleRequestPayload>(null));
    }

    [Fact]
    public void Serialize_DeclaresUtf8_SoTheUtf8WireBytesParse()
    {
        // The body is returned as a string and transmitted as UTF-8 (like every other body). A
        // StringWriter is always UTF-16, so XmlWriter used to stamp `encoding="utf-16"` into the
        // declaration - which contradicts the UTF-8 bytes actually sent. A conformant XML client
        // honoring the declaration then fails to parse the response.
        var serializer = new XmlSerializer();
        var xml = serializer.Serialize(new ExampleRequestPayload { Id = 42, Name = "foo" });

        Assert.DoesNotContain("utf-16", xml, System.StringComparison.OrdinalIgnoreCase);

        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        var doc = new System.Xml.XmlDocument();
        var exception = Record.Exception(() => doc.Load(new System.IO.MemoryStream(bytes)));
        Assert.Null(exception);
    }
}
