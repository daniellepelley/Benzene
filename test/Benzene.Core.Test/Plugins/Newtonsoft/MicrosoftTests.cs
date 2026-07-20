using System.Collections.Generic;
using Benzene.NewtonsoftJson;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Plugins.Newtonsoft;

public class SerializerTests
{

    [Fact]
    public void Serialization()
    {
        var exampleRequestPayload = new ExampleRequestPayload { Id = 42, Name = "foo" };

        var serializer = new JsonSerializer();

        var s1 = serializer.Serialize(exampleRequestPayload);
        var d1 = serializer.Deserialize(typeof(ExampleRequestPayload), s1);
        var s2 = serializer.Serialize(typeof(ExampleRequestPayload), d1);
        var result = serializer.Deserialize<ExampleRequestPayload>(s2);

        Assert.Equal(exampleRequestPayload.Id, result.Id);
        Assert.Equal(exampleRequestPayload.Name, result.Name);
    }

    private class WithDictionary
    {
        public Dictionary<string, string> Data { get; set; } = new();
    }

    [Fact]
    public void Serialization_PreservesDictionaryKeyCasing()
    {
        var payload = new WithDictionary { Data = { ["MyKey"] = "v", ["ID"] = "x" } };

        var serializer = new JsonSerializer();
        var json = serializer.Serialize(payload);

        // Property names are camelCased ("data"), but dictionary KEYS are user data and must survive
        // verbatim - camel-casing them corrupted free-form keys and the round-trip never restored them.
        Assert.Contains("\"data\"", json);
        Assert.Contains("\"MyKey\"", json);
        Assert.Contains("\"ID\"", json);

        var result = serializer.Deserialize<WithDictionary>(json);
        Assert.True(result.Data.ContainsKey("MyKey"));
        Assert.True(result.Data.ContainsKey("ID"));
        Assert.Equal("v", result.Data["MyKey"]);
    }
}
