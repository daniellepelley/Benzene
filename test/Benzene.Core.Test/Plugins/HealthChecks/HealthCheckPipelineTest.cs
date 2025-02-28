using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Extensions = Benzene.Core.MessageHandlers.Extensions;

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
            .UsingBenzene(x => x
                .AddBenzene()
                .AddBenzeneMessage()
                .AddMessageHandlers(GetType().Assembly)
            );

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline
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

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        var response = await aws.HandleAsync(request, serviceResolverFactory);

        mockHealthCheck.Verify(x => x.ExecuteAsync());

        Assert.NotNull(response);
        Assert.Contains("some-name", response.Body);
        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task Send_HealthCheck_NoName()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync())
            .ReturnsAsync(HealthCheckResult.CreateInstance(true, "HealthCheck", new Dictionary<string, object>()));

        var services = new ServiceCollection();
        services
            .UsingBenzene(x => x
                    .AddMessageHandlers(GetType().Assembly)
                    .AddBenzene()
                    .AddBenzeneMessage());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline
                .UseHealthCheck(Defaults.HealthCheckTopic, x => x
                    .AddHealthCheck(mockHealthCheck.Object)
                    .AddHealthCheck(mockHealthCheck.Object)
                    .AddHealthCheck(mockHealthCheck.Object)
                    .AddHealthCheck(new SimpleHealthCheck())
                    .AddHealthCheck(new SimpleHealthCheck())
                ).UseMessageHandlers();

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.HealthCheckTopic,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = "foo"
            })
        };

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        var response = await aws.HandleAsync(request, serviceResolverFactory);

        mockHealthCheck.Verify(x => x.ExecuteAsync());

        Assert.NotNull(response);
        Assert.Contains("HealthCheck-1", response.Body);
        Assert.Contains("HealthCheck-2", response.Body);
        Assert.Contains("HealthCheck-3", response.Body);
        Assert.Contains("Simple", response.Body);
        Assert.Contains("Simple-2", response.Body);
        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }
}
