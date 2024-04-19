using System.Threading.Tasks;
using Benzene.Core.DI;
using Benzene.Core.DirectMessage;
using Benzene.Core.MiddlewareBuilder;
using Benzene.DataAnnotations;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Plugins.DataAnnotations;

public class DataAnnotationsValidationsPipelineTest
{
    [Theory]
    [InlineData("foo", ServiceResultStatus.Ok)]
    [InlineData("foo-bar-foo-bar", ServiceResultStatus.ValidationError)]
    public async Task ValidationTest(string name, string expectedStatus)
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddDirectMessage());

        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseProcessResponse()
            .UseMessageRouter(x => x.UseDataAnnotationsValidation());

        var aws = new DirectMessageApplication(pipeline.Build());

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
