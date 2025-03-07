﻿using System.Threading.Tasks;
using Benzene.Azure.Core;
using Benzene.Azure.Kafka;
using Benzene.Azure.Kafka.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class KafkaPipelineTest
{
    [Fact]
    public async Task Send()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseKafka(kafka => kafka
                    .UseMessageHandlers()))
            .Build();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsAzureKafkaEvent();

        await app.HandleKafkaEvents(request);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }
}
