using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test;

/// <summary>
/// Covers the current-transport seam - <see cref="TransportMiddlewarePipeline{TContext}"/> setting
/// <see cref="ICurrentTransport"/> per invocation - and that a transport that wraps its pipeline (here
/// API Gateway) actually records its name, which feeds the <c>benzene.transport</c> span tag / metrics
/// dimension. Regression guard for the bug where API-Gateway/ASP.NET/self-host-HTTP/Kafka pipelines
/// reported the transport as "&lt;missing&gt;" because they never wrapped in the seam.
/// </summary>
public class CurrentTransportTest
{
    private sealed class RecordingPipeline<TContext> : IMiddlewarePipeline<TContext>
    {
        public bool Ran { get; private set; }

        public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
        {
            Ran = true;
            return Task.CompletedTask;
        }
    }

    private static void RegisterCurrentTransport(IBenzeneServiceContainer container)
    {
        container.AddScoped<CurrentTransportInfo>();
        container.AddScoped<ICurrentTransport>(x => x.GetService<CurrentTransportInfo>());
        container.AddScoped<ISetCurrentTransport>(x => x.GetService<CurrentTransportInfo>());
    }

    [Fact]
    public async Task TransportMiddlewarePipeline_RecordsTheTransport_WhenTheSetterIsRegistered()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        RegisterCurrentTransport(container);

        var inner = new RecordingPipeline<object>();
        var pipeline = new TransportMiddlewarePipeline<object>("api-gateway", inner);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.True(inner.Ran);
        Assert.Equal("api-gateway", resolver.GetService<ICurrentTransport>().Name);
    }

    [Fact]
    public async Task TransportMiddlewarePipeline_DoesNotThrow_WhenTheSetterIsNotRegistered()
    {
        // A minimal container that never called AddBenzene() has no ISetCurrentTransport. Recording the
        // transport is best-effort observability metadata, so this must run the inner pipeline, not throw.
        var services = new ServiceCollection();
        var inner = new RecordingPipeline<object>();
        var pipeline = new TransportMiddlewarePipeline<object>("api-gateway", inner);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.True(inner.Ran);
    }

    [Fact]
    public async Task ApiGatewayApplication_SetsTheCurrentTransport_ToApiGateway()
    {
        string captured = null;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        RegisterCurrentTransport(container);

        var builder = new MiddlewarePipelineBuilder<ApiGatewayContext>(container);
        builder.Use(resolver => (_, next) =>
        {
            captured = resolver.GetService<ICurrentTransport>().Name;
            return next();
        });

        var app = new ApiGatewayApplication(builder.Build());

        using var factory = new MicrosoftServiceResolverFactory(services);
        await app.HandleAsync(new APIGatewayProxyRequest { HttpMethod = "GET", Path = "/" }, factory);

        Assert.Equal(TransportNames.ApiGateway, captured);
    }
}
