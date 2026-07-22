using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.RateLimiting;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Plugins.RateLimiting;

public class RateLimitingPipelineTest
{
    private static (BenzeneMessageApplication App, MicrosoftServiceResolverFactory Resolver) CreateApp(
        Action<MiddlewarePipelineBuilder<BenzeneMessageContext>> configurePipeline)
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(
            new MicrosoftBenzeneServiceContainer(serviceCollection));
        configurePipeline(pipeline);
        pipeline.UseMessageHandlers();

        return (new BenzeneMessageApplication(pipeline.Build()),
            new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));
    }

    private static BenzeneMessageRequest CreateRequest(string name = "foo")
    {
        return MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload
        {
            Id = 42,
            Name = name,
            Mapped = "some-value"
        }).AsBenzeneMessage();
    }

    [Fact]
    public async Task UnderTheLimit_MessagesPassThrough()
    {
        var (app, resolver) = CreateApp(p => p.UseFixedWindowRateLimiting(10, TimeSpan.FromMinutes(1)));

        var response = await app.HandleAsync(CreateRequest(), resolver);

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task OverTheLimit_ShortCircuitsWithTooManyRequests()
    {
        var (app, resolver) = CreateApp(p => p.UseFixedWindowRateLimiting(1, TimeSpan.FromMinutes(1)));

        var first = await app.HandleAsync(CreateRequest(), resolver);
        var second = await app.HandleAsync(CreateRequest(), resolver);

        Assert.Equal(BenzeneResultStatus.Ok, first.StatusCode);
        Assert.Equal(BenzeneResultStatus.TooManyRequests, second.StatusCode);
        Assert.Contains("Rate limit exceeded", second.Body);
    }

    [Fact]
    public async Task PayloadSizeLimiting_RejectsAPayloadLargerThanTheBucket()
    {
        // The bucket admits at most 32 bytes at once; this payload alone is far bigger, so it can
        // never be granted - rejected outright rather than erroring.
        var (app, resolver) = CreateApp(p => p.UsePayloadSizeRateLimiting(32, 32, TimeSpan.FromMinutes(1)));

        var response = await app.HandleAsync(CreateRequest(new string('x', 200)), resolver);

        Assert.Equal(BenzeneResultStatus.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task PayloadSizeLimiting_SpendsTheByteBudget()
    {
        // Budget covers one ~44-byte payload per window but not two: first passes, second rejected.
        var (app, resolver) = CreateApp(p => p.UsePayloadSizeRateLimiting(60, 60, TimeSpan.FromMinutes(1)));

        var first = await app.HandleAsync(CreateRequest(), resolver);
        var second = await app.HandleAsync(CreateRequest(), resolver);

        Assert.Equal(BenzeneResultStatus.Ok, first.StatusCode);
        Assert.Equal(BenzeneResultStatus.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task BringYourOwnLimiter_LeaseIsReleasedAfterEachMessage()
    {
        // A concurrency limiter with a single permit: if the middleware failed to dispose the
        // lease after next(), the second sequential message would be rejected.
        using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 1,
            QueueLimit = 0,
        });
        var (app, resolver) = CreateApp(p => p.UseRateLimiting(limiter));

        var first = await app.HandleAsync(CreateRequest(), resolver);
        var second = await app.HandleAsync(CreateRequest(), resolver);

        Assert.Equal(BenzeneResultStatus.Ok, first.StatusCode);
        Assert.Equal(BenzeneResultStatus.Ok, second.StatusCode);
    }

    [Fact]
    public async Task BringYourOwnCost_IsUsedForAcquisition()
    {
        // Every message costs 5 permits against a 9-permit window: the second must be rejected.
        var (app, resolver) = CreateApp(p => p.UseRateLimiting(
            new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 9,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }),
            (_, _) => 5));

        var first = await app.HandleAsync(CreateRequest(), resolver);
        var second = await app.HandleAsync(CreateRequest(), resolver);

        Assert.Equal(BenzeneResultStatus.Ok, first.StatusCode);
        Assert.Equal(BenzeneResultStatus.TooManyRequests, second.StatusCode);
    }
}
