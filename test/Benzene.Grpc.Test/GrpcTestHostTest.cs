using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Grpc.AspNet;
using Benzene.Grpc.Test.Handlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Grpc.TestHelpers;
using Benzene.Microsoft.Dependencies;
using Benzene.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Grpc.Test;

/// <summary>
/// Proves <see cref="GrpcTestHost"/>/<c>BuildGrpcHost</c> work end to end against a real
/// <see cref="Microsoft.Extensions.Hosting.IHost"/>, exercising the exact <see cref="BenzeneStartUp"/>
/// wiring a Benzene.Grpc application uses in production (see <c>docs/getting-started-grpc.md</c>).
/// </summary>
public class GrpcTestHostTest
{
    [Fact]
    public async Task BuildGrpcHost_RoutesThroughBenzeneInterceptorAndReturnsTheHandlerResponse()
    {
        using var host = BenzeneTestHost.Create<TestStartUp>().BuildGrpcHost(endpoints => endpoints.MapGrpcService<TestGrpcService>());
        var client = new TestService.TestServiceClient(host.CreateChannel());

        var reply = await client.EchoAsync(new EchoRequest { Name = "world" });

        Assert.Equal("Hello world", reply.Message);
    }

    private class TestStartUp : BenzeneStartUp
    {
        public override IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder().Build();
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddBenzeneGrpc();
            services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(new[] { typeof(EchoMessageHandler) }).AddGrpcMessageHandlers());
        }

        public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
        {
            app.UseGrpc(grpc => grpc.UseMessageHandlers(typeof(EchoMessageHandler)));
        }
    }
}
