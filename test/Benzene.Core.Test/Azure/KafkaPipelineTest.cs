using System.Threading.Tasks;
using Benzene.Azure.Core;
using Benzene.Azure.Kafka;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Test.Examples;
using Benzene.Tools;
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

        var app = new InlineAzureStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseKafka(kafka => kafka
                    .UseProcessResponse()
                    .UseMessageRouter()))
            .Build();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsAzureKafkaEvent();

        await app.HandleKafkaEvents(request);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }
}
