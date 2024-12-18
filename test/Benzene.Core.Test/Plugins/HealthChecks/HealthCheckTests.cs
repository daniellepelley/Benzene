using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Benzene.Core.MessageHandling;

namespace Benzene.Test.Plugins.HealthChecks;

public class HealthCheckTests
{
    [Fact]
    public async Task SimpleHealthCheck()
    {
        var sim = new SimpleHealthCheck();
        var result = await sim.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task HandlesExceptions()
    {
        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.ExecuteAsync()).Throws<Exception>();

        var result =  await HealthCheckProcessor.PerformHealthChecksAsync(Defaults.HealthCheckTopic, new []{ mockHealthCheck.Object });

        var healthCheckResult = result.Payload as HealthCheckResponse;
        Assert.False(healthCheckResult.IsHealthy);
    }

    [Fact]
    public async Task HealthCheckBuilderTest()
    {
        var serviceCollection = new ServiceCollection();

        var container = new MicrosoftBenzeneServiceContainer(serviceCollection);
        container.AddBenzene().AddBenzeneMessage().AddMessageHandlers();

        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        middlewarePipelineBuilder
            .UseProcessResponse()
            .UseHealthCheck("healthcheck", x => x
                .AddHealthCheck<SimpleHealthCheck>()
                .AddHealthCheck(new SimpleHealthCheck())
                .AddHealthCheckFactory(new SimpleHealthCheckFactory())
                .AddHealthCheck(_ => new SimpleHealthCheck())
                .AddHealthCheck(resolver => resolver.GetService<SimpleHealthCheck>())
                .AddHealthCheck("inline", _ => Task.FromResult(HealthCheckResult.CreateInstance(false)))
                .AddHealthCheck("inline", _ => HealthCheckResult.CreateInstance(false))
                .AddHealthCheck(async _ => await Task.FromResult(HealthCheckResult.CreateInstance(false)))
                .AddHealthCheck(_ => HealthCheckResult.CreateInstance(false))
                .AddHealthCheck("inline", _ => Task.FromResult(false))
                .AddHealthCheck("inline", _ => false)
                .AddHealthCheck(async _ => await Task.FromResult(false))
                .AddHealthCheck(_ => false));

        
        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Topic = "healthcheck" });

        await middlewarePipelineBuilder.Build().HandleAsync(context, new MicrosoftServiceResolverAdapter(serviceCollection.BuildServiceProvider()));

        Assert.NotNull(context.BenzeneMessageResponse);

        var response = JsonConvert.DeserializeObject<HealthCheckResponse>(context.BenzeneMessageResponse.Body);
        Assert.Equal(13, response.HealthChecks.Count);
    }
    
    [Fact]
    public async Task HealthCheckBuilder_WithExceptions_Test()
    {
        var serviceCollection = new ServiceCollection();

        var container = new MicrosoftBenzeneServiceContainer(serviceCollection);
        container.AddBenzene().AddBenzeneMessage().AddMessageHandlers();

        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        middlewarePipelineBuilder
            .UseProcessResponse()
            .UseHealthCheck("healthcheck", x => x
                .AddHealthCheck<ExceptionThrowingHealthCheck>()
                .AddHealthCheck(new ExceptionThrowingHealthCheck())
                .AddHealthCheckFactory(new ExceptionThrowingHealthCheckFactory())
                .AddHealthCheck(_ => new ExceptionThrowingHealthCheck())
                .AddHealthCheck(_ => throw new Exception()));
        
        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Topic = "healthcheck" });

        await middlewarePipelineBuilder.Build().HandleAsync(context, new MicrosoftServiceResolverAdapter(serviceCollection.BuildServiceProvider()));

        Assert.NotNull(context.BenzeneMessageResponse);

        var response = JsonConvert.DeserializeObject<HealthCheckResponse>(context.BenzeneMessageResponse.Body);
        Assert.Equal(5, response.HealthChecks.Count);
    }

}

public class ExceptionThrowingHealthCheckFactory : IHealthCheckFactory
{
    public IHealthCheck Create(IServiceResolver resolver)
    {
        throw new Exception();
    }
}

public class ExceptionThrowingHealthCheck : IHealthCheck
{
    public string Type { get; }
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        throw new Exception();
    }
}


