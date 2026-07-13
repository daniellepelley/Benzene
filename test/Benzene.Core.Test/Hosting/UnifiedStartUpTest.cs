using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.HostedService;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.Test.Examples;
using Benzene.Tools.Aws;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Benzene.Test.Hosting;

public class SharedStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage()
            .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseAwsLambda(aws => aws.UseBenzeneMessage(p => p.UseMessageHandlers()))
        .UseWorker(w => w.Add(_ => new FakeWorker()));
}

public class FakeWorker : IBenzeneWorker
{
    private static bool _started;
    private static bool _stopped;

    public static bool Started => _started;
    public static bool Stopped => _stopped;

    public static void Reset()
    {
        _started = false;
        _stopped = false;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stopped = true;
        return Task.CompletedTask;
    }
}

public class UnifiedStartUpTest
{
    [Fact]
    public async Task AwsLambdaHost_CanBeInstantiated()
    {
        using var host = new AwsLambdaHost<SharedStartUp>();
        Assert.NotNull(host);
    }

    [Fact]
    public async Task GenericHost_CanBeBuilt()
    {
        var host = new HostBuilder()
            .UseBenzene<SharedStartUp>()
            .Build();

        Assert.NotNull(host);
        var hostedServices = host.Services.GetServices<IHostedService>();
        Assert.NotEmpty(hostedServices);
    }

    [Fact]
    public async Task GenericHost_StartsAndStopsWorker()
    {
        FakeWorker.Reset();

        var host = new HostBuilder()
            .UseBenzene<SharedStartUp>()
            .Build();

        var hostedServices = host.Services.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);

        foreach (var service in hostedServices)
        {
            await service.StartAsync(CancellationToken.None);
        }

        Assert.True(FakeWorker.Started);

        foreach (var service in hostedServices)
        {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.True(FakeWorker.Stopped);
    }

    [Fact]
    public void AwsLambdaHost_PlatformIdentifier()
    {
        // Verify the platform identifier is correct
        var app = new Benzene.Aws.Lambda.Core.AwsLambdaApplicationBuilder(
            new Benzene.Core.Middleware.MiddlewarePipelineBuilder<Benzene.Aws.Lambda.Core.AwsEventStream.AwsEventStreamContext>(
                new Benzene.Core.Middleware.NullBenzeneServiceContainer()),
            new Benzene.Core.Middleware.NullBenzeneServiceContainer());

        Assert.Equal("AwsLambda", app.Platform);
    }

    [Fact]
    public void WorkerApplicationBuilder_PlatformIdentifier()
    {
        // Verify the platform identifier is correct
        var app = new WorkerApplicationBuilder(new Benzene.Core.Middleware.NullBenzeneServiceContainer());
        Assert.Equal("Worker", app.Platform);
    }

    [Fact]
    public void UseAwsLambda_NoOpOnOtherPlatforms()
    {
        // Verify that UseAwsLambda is a no-op on non-AWS platforms
        var worker = new WorkerApplicationBuilder(new Benzene.Core.Middleware.NullBenzeneServiceContainer());
        var result = worker.UseAwsLambda(_ => { });
        Assert.NotNull(result);
        Assert.Equal("Worker", result.Platform);
    }

    [Fact]
    public void UseWorker_NoOpOnOtherPlatforms()
    {
        // Verify that UseWorker is a no-op on non-Worker platforms
        var aws = new Benzene.Aws.Lambda.Core.AwsLambdaApplicationBuilder(
            new Benzene.Core.Middleware.MiddlewarePipelineBuilder<Benzene.Aws.Lambda.Core.AwsEventStream.AwsEventStreamContext>(
                new Benzene.Core.Middleware.NullBenzeneServiceContainer()),
            new Benzene.Core.Middleware.NullBenzeneServiceContainer());
        var result = aws.UseWorker(_ => { });
        Assert.NotNull(result);
        Assert.Equal("AwsLambda", result.Platform);
    }
}
