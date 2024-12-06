using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Core.Response;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Benzene.Core.MessageHandling;

namespace Benzene.Test.Plugins.HealthChecks;

public class HealthCheckPipelineTest
{
    [Fact]
    public async Task Send_HealthCheck()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync())
            .ReturnsAsync(HealthCheckResult.CreateInstance(true, "some-name", new Dictionary<string, object>()));
        mockHealthCheck.Setup(x => x.Type)
            .Returns("some-name");

        var services = new ServiceCollection();
        services
            .AddTransient<ResponseMiddleware<BenzeneMessageContext>>()
            .UsingBenzene(x => x
                .AddBenzene()
                .AddBenzeneMessage()
                .AddMessageHandlers(GetType().Assembly)
            );

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline
            .UseProcessResponse()
            .UseHealthCheck(Defaults.HealthCheckTopic, x => x.AddHealthCheck(mockHealthCheck.Object))
            .UseMessageHandlers();

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.HealthCheckTopic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = "foo"
            })
        };

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
        var response = await aws.HandleAsync(request, serviceResolver);

        mockHealthCheck.Verify(x => x.ExecuteAsync());

        Assert.NotNull(response);
        Assert.Contains("some-name", response.Body);
        Assert.Equal(ServiceResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task Send_HealthCheck_NoName()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync())
            .ReturnsAsync(HealthCheckResult.CreateInstance(true, "HealthCheck", new Dictionary<string, object>()));

        var services = new ServiceCollection();
        services
            .AddTransient<ResponseMiddleware<BenzeneMessageContext>>()
            .UsingBenzene(x => x
                .AddBenzene()
                .AddBenzeneMessage()
                .AddMessageHandlers(GetType().Assembly)
            );

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline
            .UseProcessResponse()
            .UseHealthCheck(Defaults.HealthCheckTopic, x => x
                .AddHealthCheck(mockHealthCheck.Object)
                .AddHealthCheck(mockHealthCheck.Object)
                .AddHealthCheck(mockHealthCheck.Object)
                .AddHealthCheck(new SimpleHealthCheck())
                .AddHealthCheck(new SimpleHealthCheck())
            )
            .UseMessageHandlers();

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.HealthCheckTopic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = "foo"
            })
        };

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
        var response = await aws.HandleAsync(request, serviceResolver);

        mockHealthCheck.Verify(x => x.ExecuteAsync());

        Assert.NotNull(response);
        Assert.Contains("HealthCheck-1", response.Body);
        Assert.Contains("HealthCheck-2", response.Body);
        Assert.Contains("HealthCheck-3", response.Body);
        Assert.Contains("Simple", response.Body);
        Assert.Contains("Simple-2", response.Body);
        Assert.Equal(ServiceResultStatus.Ok, response.StatusCode);
    }
}
