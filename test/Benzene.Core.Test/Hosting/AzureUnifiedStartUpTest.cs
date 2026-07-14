using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.AspNet.TestHelpers;
using Benzene.Azure.Function.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Benzene.Test.Hosting;

// Covers the isolated-worker host-builder glue that HostBuilderExtensions.UseBenzene<TStartUp>()/
// AzureFunctionAppBuilderExtensions.UseBenzeneInvocation()/FunctionsWorkerApplicationBuilderExtensions.UseBenzene()
// added for the cross-platform BenzeneStartUp unification -- none of it was previously exercised by any
// test (BenzeneTestHostTest's BuildAzureFunctionApp_HandlesHttpRequest goes through the separate
// Benzene.Testing test-host path, not this real Program.cs entry point).
public class AzureInvocationRegistrationStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x.AddBenzene());

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseBenzeneInvocation();
}

public class AzureUnifiedStartUpTest
{
    [Fact]
    public async Task IHostBuilderUseBenzene_BuildsWorkingAzureFunctionApp()
    {
        var mockExampleService = new Mock<IExampleService>();

        var host = new HostBuilder()
            .UseBenzene<AzureBenzeneTestHostStartUp>()
            .ConfigureServices(services => services.AddSingleton(mockExampleService.Object))
            .Build();

        using var scope = host.Services.CreateScope();
        var app = scope.ServiceProvider.GetRequiredService<IAzureFunctionApp>();
        Assert.NotNull(app);

        var request = HttpBuilder.Create(Defaults.Method, Defaults.Path, Defaults.MessageAsObject).AsAspNetCoreHttpRequest();
        var response = await app.HandleHttpRequest(request) as ContentResult;

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public void UseBenzeneInvocation_RegistersInvocationAccessor()
    {
        var host = new HostBuilder()
            .UseBenzene<AzureInvocationRegistrationStartUp>()
            .Build();

        using var scope = host.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetService<IBenzeneInvocationAccessor>();

        Assert.NotNull(accessor);
        Assert.Null(accessor.Invocation);
    }

    [Fact]
    public async Task FunctionsWorkerUseBenzene_PopulatesInvocationForDuration()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneInvocation());
        var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<IBenzeneInvocationAccessor>();

        var context = new Mock<FunctionContext>();
        context.Setup(x => x.InvocationId).Returns("test-invocation-id");
        context.Setup(x => x.InstanceServices).Returns(serviceProvider);

        Func<FunctionExecutionDelegate, FunctionExecutionDelegate> capturedUse = null;
        var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
        builder.Setup(x => x.Use(It.IsAny<Func<FunctionExecutionDelegate, FunctionExecutionDelegate>>()))
            .Callback<Func<FunctionExecutionDelegate, FunctionExecutionDelegate>>(f => capturedUse = f)
            .Returns(builder.Object);

        builder.Object.UseBenzene();
        Assert.NotNull(capturedUse);

        IBenzeneInvocation invocationDuringNext = null;
        FunctionExecutionDelegate next = _ =>
        {
            invocationDuringNext = accessor.Invocation;
            return Task.CompletedTask;
        };

        var executionDelegate = capturedUse(next);
        await executionDelegate(context.Object);

        Assert.NotNull(invocationDuringNext);
        Assert.Equal("test-invocation-id", invocationDuringNext.InvocationId);
        Assert.Equal("AzureFunctions", invocationDuringNext.Platform);
        Assert.Same(context.Object, invocationDuringNext.GetFeature<FunctionContext>());
    }
}
