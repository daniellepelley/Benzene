using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Http.BenzeneMessage;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Xunit;

namespace Benzene.Test.Http;

public class BenzeneMessageHttpPipelineTest
{
    private static AwsLambdaBenzeneTestHost CreateHost(BenzeneMessageHttpOptions? options = null)
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseBenzeneMessage(options ?? new BenzeneMessageHttpOptions(),
                        messageApp => messageApp.UseMessageHandlers())
                    .UseMessageHandlers()
                )
            )
            .BuildHost();
    }

    private static object CreateEnvelope(string topic)
    {
        return new
        {
            topic,
            headers = new Dictionary<string, string>(),
            body = Defaults.Message
        };
    }

    [Fact]
    public async Task PostEnvelope_DispatchesThroughMessagePipeline()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", "/benzene-message", CreateEnvelope(Defaults.Topic)));

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.Contains("\"statusCode\":\"ok\"", response.Body);
    }

    [Fact]
    public async Task PostEnvelope_UnknownTopic_MapsEnvelopeStatusToHttpStatus()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", "/benzene-message", CreateEnvelope("no-such-topic")));

        Assert.NotNull(response);
        Assert.NotEqual(200, response.StatusCode);
    }

    [Fact]
    public async Task PostEnvelope_TopicRejectedByFilter_RespondsNotFound()
    {
        var host = CreateHost(new BenzeneMessageHttpOptions { TopicFilter = topic => topic != Defaults.Topic });

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("POST", "/benzene-message", CreateEnvelope(Defaults.Topic)));

        Assert.NotNull(response);
        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public async Task OtherRequests_FallThroughToHttpMessageHandlers()
    {
        var host = CreateHost();

        var response = await host.SendApiGatewayAsync(
            HttpBuilder.Create("GET", Defaults.Path, Defaults.MessageAsObject));

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
    }
}
