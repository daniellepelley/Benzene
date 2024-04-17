using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Core.DirectMessage;
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
            .UsingBenzene(x => x.AddServiceResolver())
            .AddTransient<DirectMessageMapper>()
            .AddSingleton<MultiSerializerOptionsRequestMapper<DirectMessageContext, JsonSerializer>>()
            .AddSingleton<IMessageMapper<DirectMessageContext>, DirectMessageMapper>()
            .AddSingleton<JsonSerializer>()
            .AddSingleton<ISerializerOption<DirectMessageContext>>(_ =>
                new SerializerOption<DirectMessageContext, JsonSerializer>(_ => true));

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var sut = serviceResolver.GetService<MultiSerializerOptionsRequestMapper<DirectMessageContext, JsonSerializer>>();

        var request = new DirectMessageRequest
        {
            Topic = Defaults.Topic,
            Message = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = Defaults.Name
            }),
            Headers = null
        };

        var mappedRequest = sut.GetBody<ExampleRequestPayload>(DirectMessageContext.CreateInstance(request));

        Assert.Equal(Defaults.Name, mappedRequest.Name);
    }
}
