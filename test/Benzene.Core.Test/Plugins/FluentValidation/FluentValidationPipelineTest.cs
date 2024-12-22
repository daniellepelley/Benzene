using System.Threading.Tasks;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.FluentValidation;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Benzene.Core.MessageHandling;

namespace Benzene.Test.Plugins.FluentValidation;

public class FluentValidationPipelineTest
{
    [Theory]
    [InlineData("foo", ServiceResultStatus.Ok)]
    [InlineData("foo-bar-foo-bar", ServiceResultStatus.ValidationError)]
    public async Task ValidationTest(string name, string expectedStatus)
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseMessageHandlers(x => x.UseFluentValidation());

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.Topic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = name 
            })
        };

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverAdapter(serviceCollection.BuildServiceProvider()));

        Assert.NotNull(response);
        Assert.Equal(expectedStatus, response.StatusCode);
    }
}
