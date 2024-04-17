using System.Reflection;
using System.Threading.Tasks;
using Benzene.Core.DI;
using Benzene.Core.DirectMessage;
using Benzene.Core.Filters;
using Benzene.Core.MiddlewareBuilder;
using Benzene.FluentValidation;
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
    [InlineData("foo", ServiceResultStatus.Ok)]
    [InlineData("foo-bar-foo-bar", ServiceResultStatus.Ignored)]
    public async Task Send_HealthCheck(string name, string expectedStatus)
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddDirectMessage());

        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseProcessResponse()
            .UseMessageRouter(x => x.UseFilters());

        var aws = new DirectMessageApplication(pipeline.AsPipeline());

        var request = new DirectMessageRequest
        {
            Topic = Defaults.Topic,
            Message = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = name 
            })
        };

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverAdapter(serviceCollection.BuildServiceProvider()));

        Assert.NotNull(response);
        Assert.Equal(expectedStatus, response.StatusCode);
    }
}
