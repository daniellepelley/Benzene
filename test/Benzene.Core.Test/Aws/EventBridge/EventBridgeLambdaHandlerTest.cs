using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.EventBridge.TestHelpers;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.EventBridge;

public class EventBridgeLambdaHandlerTest
{
    [Fact]
    public async Task EventBridgePayload_IsHandled()
    {
        EventBridgeContext handledContext = null;

        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        app.UseEventBridge(eventBridge => eventBridge
            .Use(null, (context, next) =>
            {
                handledContext = context;
                return next();
            })
        );

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsEventBridge();

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.NotNull(handledContext);
        Assert.Equal(Defaults.Topic, handledContext.Event.DetailType);
    }

    [Fact]
    public async Task NonEventBridgePayload_FallsThroughToNextMiddleware()
    {
        var eventBridgeHandled = false;
        var fellThrough = false;

        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        app.UseEventBridge(eventBridge => eventBridge
            .Use(null, (context, next) =>
            {
                eventBridgeHandled = true;
                return next();
            })
        );
        app.Use(null, (context, next) =>
        {
            fellThrough = true;
            return next();
        });

        // A BenzeneMessage-shaped payload: has a topic, but no detail-type/source.
        var request = new { topic = Defaults.Topic, headers = new { }, body = "{}" };

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.False(eventBridgeHandled);
        Assert.True(fellThrough);
    }

    [Fact]
    public async Task DetailTypeWithoutSource_FallsThroughToNextMiddleware()
    {
        var eventBridgeHandled = false;
        var fellThrough = false;

        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        app.UseEventBridge(eventBridge => eventBridge
            .Use(null, (context, next) =>
            {
                eventBridgeHandled = true;
                return next();
            })
        );
        app.Use(null, (context, next) =>
        {
            fellThrough = true;
            return next();
        });

        // detail-type present but source missing - not a valid EventBridge payload.
        var request = new Dictionary<string, object> { ["detail-type"] = Defaults.Topic, ["detail"] = new { } };

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.False(eventBridgeHandled);
        Assert.True(fellThrough);
    }

    [Fact]
    public async Task SourceWithoutDetailType_FallsThroughToNextMiddleware()
    {
        var eventBridgeHandled = false;
        var fellThrough = false;

        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        app.UseEventBridge(eventBridge => eventBridge
            .Use(null, (context, next) =>
            {
                eventBridgeHandled = true;
                return next();
            })
        );
        app.Use(null, (context, next) =>
        {
            fellThrough = true;
            return next();
        });

        // source present but detail-type missing - not a valid EventBridge payload.
        var request = new { source = "com.example.orders", detail = new { } };

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.False(eventBridgeHandled);
        Assert.True(fellThrough);
    }
}
