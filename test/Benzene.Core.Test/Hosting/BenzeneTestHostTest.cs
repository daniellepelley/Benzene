using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.AspNet.TestHelpers;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Core.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Hosting;

public class AwsBenzeneTestHostStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddBenzeneMessage()
            .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseAwsLambda(aws => aws.UseBenzeneMessage(p => p.UseMessageHandlers()));
}

public class AzureBenzeneTestHostStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly)
            .AddHttpMessageHandlers());

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseHttp(http => http.UseMessageHandlers());
}

public class AwsApiGatewayBenzeneTestHostStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly)
            .AddHttpMessageHandlers());

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseAwsLambda(aws => aws.UseApiGateway(apiGateway => apiGateway.UseMessageHandlers()));
}

public class BenzeneTestHostTest
{
    [Fact]
    public async Task BuildAwsLambdaHost_SendsBenzeneMessage()
    {
        var mockExampleService = new Mock<IExampleService>();

        var host = new AwsLambdaBenzeneTestHost(
            BenzeneTestHost.Create<AwsBenzeneTestHostStartUp>()
                .WithServices(s => s.AddSingleton(mockExampleService.Object))
                .BuildAwsLambdaHost());

        var message = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject);
        await host.SendBenzeneMessageAsync(message);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task BuildAwsLambdaHost_SendApiGateway_CompilesAndRunsAsAOneLiner()
    {
        // The gold-standard shape: Create<StartUp>() -> WithServices(fake) -> BuildAwsLambdaHost()
        // -> SendApiGatewayAsync(...), with NO manual `new AwsLambdaBenzeneTestHost(...)` wrap. The
        // Send* overload extending the returned IAwsLambdaEntryPoint is what makes this compile,
        // matching GCP's BuildGooglePubSubFunctionHost().SendPubSubAsync(...).
        var mockExampleService = new Mock<IExampleService>();

        APIGatewayProxyResponse response = await BenzeneTestHost.Create<AwsApiGatewayBenzeneTestHostStartUp>()
            .WithServices(s => s.AddSingleton(mockExampleService.Object))
            .BuildAwsLambdaHost()
            .SendApiGatewayAsync(HttpBuilder.Create(Defaults.Method, Defaults.Path, Defaults.MessageAsObject));

        Assert.Equal(200, response.StatusCode);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task BuildAwsLambdaHost_SendBenzeneMessage_CompilesAndRunsAsAOneLiner()
    {
        // Same one-liner ergonomics for the transport-neutral BenzeneMessage entry point.
        var mockExampleService = new Mock<IExampleService>();

        var response = await BenzeneTestHost.Create<AwsBenzeneTestHostStartUp>()
            .WithServices(s => s.AddSingleton(mockExampleService.Object))
            .BuildAwsLambdaHost()
            .SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject));

        Assert.NotNull(response);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task BuildAzureFunctionApp_HandlesHttpRequest()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = BenzeneTestHost.Create<AzureBenzeneTestHostStartUp>()
            .WithServices(s => s.AddSingleton(mockExampleService.Object))
            .BuildAzureFunctionApp();

        var request = HttpBuilder.Create(Defaults.Method, Defaults.Path, Defaults.MessageAsObject).AsAspNetCoreHttpRequest();
        var response = await app.HandleHttpRequest(request) as ContentResult;

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public void WithConfiguration_OverridesConfigurationValue()
    {
        var value = BenzeneTestHost.Create<AzureBenzeneTestHostStartUp>()
            .WithConfiguration("some-key", "some-value")
            .Build((_, _, configuration) => configuration["some-key"]);

        Assert.Equal("some-value", value);
    }

    [Fact]
    public void WithServices_OverrideAppliesBeforePlatformBridgeRuns()
    {
        var mockExampleService = new Mock<IExampleService>();

        var resolvedService = BenzeneTestHost.Create<AzureBenzeneTestHostStartUp>()
            .WithServices(s => s.AddSingleton(mockExampleService.Object))
            .Build((_, services, _) => services.BuildServiceProvider().GetService<IExampleService>());

        Assert.Same(mockExampleService.Object, resolvedService);
    }
}
