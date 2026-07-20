using System;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Core.Core.BenzeneMessage;

public class BenzeneMessageResponseSuppressionTest
{
    private static async Task<IBenzeneMessageResponse> RunAsync(bool suppressResponse)
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));
        if (suppressResponse)
        {
            pipeline.SuppressResponse();
        }

        pipeline.UseMessageHandlers(typeof(ExampleMessageHandler));

        var application = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.Topic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload { Name = "foo" })
        };

        return await application.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));
    }

    [Fact]
    public async Task WithoutSuppression_WritesResponseStatusAndBody()
    {
        var response = await RunAsync(suppressResponse: false);

        Assert.False(string.IsNullOrEmpty(response.StatusCode));
        Assert.NotNull(response.Body);
    }

    [Fact]
    public async Task WithSuppression_LeavesResponseUnwritten()
    {
        var response = await RunAsync(suppressResponse: true);

        Assert.True(string.IsNullOrEmpty(response.StatusCode));
        Assert.Null(response.Body);
    }

    [Fact]
    public void SuppressionMiddleware_SetsTheScopedFlag()
    {
        var suppression = new BenzeneMessageResponseSuppression();
        var middleware = new SuppressBenzeneMessageResponseMiddleware(suppression);

        Func<Task> next = () => Task.CompletedTask;
        middleware.HandleAsync(new BenzeneMessageContext(new BenzeneMessageRequest()), next);

        Assert.True(suppression.IsSuppressed);
    }
}
