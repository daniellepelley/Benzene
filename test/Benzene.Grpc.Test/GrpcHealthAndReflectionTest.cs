using Benzene.Core.MessageHandlers.DI;
using Benzene.Grpc.AspNet;
using Benzene.Grpc.Test.Handlers;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Grpc.Reflection.V1Alpha;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcHealthAndReflectionTest
{
    [Fact]
    public void BenzeneGrpcOptions_DefaultsToHealthChecksAndReflectionDisabled()
    {
        var options = new BenzeneGrpcOptions();

        Assert.False(options.EnableHealthChecks);
        Assert.False(options.EnableReflection);
    }

    [Fact]
    public async Task Health_WhenNoBenzeneHealthChecksAreRegistered_ReturnsServing()
    {
        using var host = await BuildHostAsync(enableHealthChecks: true, enableReflection: false, registerFailingCheck: false);
        var client = new Health.HealthClient(CreateChannel(host));

        var response = await client.CheckAsync(new HealthCheckRequest());

        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, response.Status);
    }

    [Fact]
    public async Task Health_WhenABenzeneHealthCheckFails_ReturnsNotServing()
    {
        using var host = await BuildHostAsync(enableHealthChecks: true, enableReflection: false, registerFailingCheck: true);
        var client = new Health.HealthClient(CreateChannel(host));

        var response = await client.CheckAsync(new HealthCheckRequest());

        Assert.Equal(HealthCheckResponse.Types.ServingStatus.NotServing, response.Status);
    }

    [Fact]
    public async Task Reflection_WhenEnabled_ListsTheHostedService()
    {
        using var host = await BuildHostAsync(enableHealthChecks: false, enableReflection: true, registerFailingCheck: false);
        var client = new ServerReflection.ServerReflectionClient(CreateChannel(host));

        using var call = client.ServerReflectionInfo();
        await call.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = string.Empty });
        Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
        var services = call.ResponseStream.Current.ListServicesResponse.Service.Select(s => s.Name).ToArray();
        await call.RequestStream.CompleteAsync();

        Assert.Contains("benzene.test.TestService", services);
    }

    private static async Task<IHost> BuildHostAsync(bool enableHealthChecks, bool enableReflection, bool registerFailingCheck)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddBenzeneGrpc(o =>
                    {
                        o.EnableHealthChecks = enableHealthChecks;
                        o.EnableReflection = enableReflection;
                    });
                    if (registerFailingCheck)
                    {
                        services.AddScoped<IHealthCheck, FailingHealthCheck>();
                    }
                    services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(new[] { typeof(EchoMessageHandler) }).AddGrpcMessageHandlers());
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<TestGrpcService>();
                        if (enableHealthChecks)
                        {
                            endpoints.MapBenzeneGrpcHealthService();
                        }
                        if (enableReflection)
                        {
                            endpoints.MapBenzeneGrpcReflectionService();
                        }
                    });
                    app.UseBenzene(x => x.UseGrpc(grpc => grpc.UseMessageHandlers(typeof(EchoMessageHandler))));
                });
            });

        return await hostBuilder.StartAsync();
    }

    private static GrpcChannel CreateChannel(IHost host)
    {
        var testServer = host.GetTestServer();
        return GrpcChannel.ForAddress(testServer.BaseAddress ?? new Uri("http://localhost"), new GrpcChannelOptions
        {
            HttpHandler = testServer.CreateHandler()
        });
    }

    private class FailingHealthCheck : IHealthCheck
    {
        public string Type => "fake-failing-check";

        public Task<IHealthCheckResult> ExecuteAsync()
        {
            return Task.FromResult(HealthCheckResult.CreateInstance(false, Type));
        }
    }
}
