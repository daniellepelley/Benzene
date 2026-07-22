using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.DryIoc;
using Benzene.Microsoft.Dependencies;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.DryIoc.Test;

/// <summary>
/// End-to-end proof that Benzene runs unchanged on containers other than the two it ships adapters for:
/// <list type="bullet">
/// <item>the native <see cref="DryIocServiceResolverFactory"/> (the <c>Benzene.DryIoc</c> adapter), and</item>
/// <item>a <b>third-party <see cref="System.IServiceProvider"/></b> built by DryIoc's MS DI integration,
/// driving Benzene through the existing <see cref="MicrosoftServiceResolverFactory"/> — the "bring your
/// own container" path, needing no new Benzene code since that factory already accepts an
/// externally-built provider.</item>
/// </list>
/// Both run the same message through a real <see cref="BenzeneMessageApplication"/>, so the container
/// genuinely resolves Benzene's message-handling services.
/// </summary>
public class DryIocAndByoProviderPipelineTest
{
    private const string OkTopic = "ping";

    private static BenzeneMessageApplication BuildApp()
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(
            new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(null, (context, next) =>
        {
            context.BenzeneMessageResponse = new BenzeneMessageResponse
            {
                Body = context.BenzeneMessageRequest.Body,
                Headers = context.BenzeneMessageRequest.Headers,
                StatusCode = context.BenzeneMessageRequest.Topic == OkTopic ? "200" : "503"
            };
            return next();
        });

        return new BenzeneMessageApplication(pipeline.Build());
    }

    private static void ConfigureBenzene(IBenzeneServiceContainer container)
    {
        container.AddBenzene();
        container.AddMessageHandlers(typeof(DryIocAndByoProviderPipelineTest).Assembly);
        container.AddScoped<BenzeneMessageGetter>();
    }

    private static BenzeneMessageRequest CreateRequest()
        => new() { Topic = OkTopic, Body = "hello", Headers = new Dictionary<string, string>() };

    [Fact]
    public async Task DryIocNativeAdapter_ResolvesAndRunsThePipeline()
    {
        var container = Benzene.DryIoc.Extensions.CreateContainer().UsingBenzene(ConfigureBenzene);
        using var factory = new DryIocServiceResolverFactory(container);

        var response = await BuildApp().HandleAsync(CreateRequest(), factory);

        Assert.NotNull(response);
        Assert.Equal("hello", response.Body);
        Assert.Equal("200", response.StatusCode);
    }

    [Fact]
    public async Task ByoServiceProvider_FromDryIoc_ResolvesAndRunsThePipeline()
    {
        // Register Benzene into a standard IServiceCollection exactly as any app would...
        var services = new ServiceCollection();
        services.UsingBenzene(ConfigureBenzene);

        // ...but build the IServiceProvider with a THIRD-PARTY container (DryIoc), not Microsoft's.
        var container = new Container(rules => rules.WithMicrosoftDependencyInjectionRules());
        System.IServiceProvider serviceProvider = container.WithDependencyInjectionAdapter(services);

        // Benzene resolves through that provider via the existing Microsoft adapter — no Benzene changes.
        using var factory = new MicrosoftServiceResolverFactory(serviceProvider);

        var response = await BuildApp().HandleAsync(CreateRequest(), factory);

        Assert.NotNull(response);
        Assert.Equal("hello", response.Body);
        Assert.Equal("200", response.StatusCode);
    }
}
