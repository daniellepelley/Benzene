using System.Threading.Tasks;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Azure.Function.ServiceBus.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class ServiceBusPipelineTest
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
                .UseServiceBus(serviceBus => serviceBus
                    .UseMessageHandlers()))
            .Build();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsAzureServiceBusMessage();

        await app.HandleServiceBusMessages(request);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }
}
