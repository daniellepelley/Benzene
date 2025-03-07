﻿using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Extras.Request;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.Test.Core.Core.Mappers;

public class MappersTest
{
    [Fact(Skip = "Review")]
    public void Map_Json()
    {
        var services = new ServiceCollection();
        services
            .AddTransient<BenzeneMessageGetter>()
            .AddSingleton<MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>>()
            .AddSingleton<IMessageGetter<BenzeneMessageContext>, BenzeneMessageGetter>()
            .AddSingleton<JsonSerializer>()
            .AddSingleton<ISerializerOption<BenzeneMessageContext>>(_ =>
                new SerializerOption<BenzeneMessageContext, JsonSerializer>(x => x.Always()));

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
