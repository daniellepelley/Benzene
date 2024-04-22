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
}
