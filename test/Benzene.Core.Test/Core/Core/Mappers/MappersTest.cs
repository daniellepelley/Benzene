using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Request;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = Benzene.Core.Serialization.JsonSerializer;

namespace Benzene.Test.Core.Core.Mappers;

public class MappersTest
{
    [Fact(Skip = "Review")]
    public void Map_Json()
    {
        var services = new ServiceCollection();
        services
            .AddTransient<BenzeneMessageMapper>()
            .AddSingleton<MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>>()
            .AddSingleton<IMessageMapper<BenzeneMessageContext>, BenzeneMessageMapper>()
            .AddSingleton<JsonSerializer>()
            .AddSingleton<ISerializerOption<BenzeneMessageContext>>(_ =>
                new SerializerOption<BenzeneMessageContext, JsonSerializer>(_ => true));

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var sut = serviceResolver.GetService<MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>>();

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.Topic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = Defaults.Name
            }),
            Headers = null
        };

        var mappedRequest = sut.GetBody<ExampleRequestPayload>(new BenzeneMessageContext(request));

        Assert.Equal(Defaults.Name, mappedRequest.Name);
    }
}
