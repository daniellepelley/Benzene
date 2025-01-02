using System.Threading.Tasks;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Filters;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Core.Core.Filters;

public class FiltersPipelineTest
{
    [Theory]
    [InlineData("foo", BenzeneResultStatus.Ok)]
    [InlineData("foo-bar-foo-bar", BenzeneResultStatus.Ignored)]
    public async Task Send_HealthCheck(string name, string expectedStatus)
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseMessageHandlers(x => x.UseFilters());

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.Topic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = name 
            })
        };

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.NotNull(response);
        Assert.Equal(expectedStatus, response.StatusCode);
    }
}
