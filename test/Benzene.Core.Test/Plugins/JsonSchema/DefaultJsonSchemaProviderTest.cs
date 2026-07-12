using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.JsonSchema;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Plugins.JsonSchema;

public class DefaultJsonSchemaProviderTest
{
    private static BenzeneMessageApplication CreateApplication(out IServiceCollection serviceCollection)
    {
        serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseJsonSchema()
            .UseMessageHandlers();

        return new BenzeneMessageApplication(pipeline.Build());
    }

    [Fact]
    public async Task GeneratedSchema_ValidPayload_PassesThrough()
    {
        var app = CreateApplication(out var serviceCollection);

        var request = MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload
        {
            Id = 42,
            Name = "foo",
            Mapped = "some-value"
        }).AsBenzeneMessage();

        var response = await app.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task GeneratedSchema_TypeMismatch_ReturnsValidationError()
    {
        var app = CreateApplication(out var serviceCollection);

        var request = MessageBuilder.Create(Defaults.Topic, new
        {
            id = "not-a-number",
            name = "foo",
            mapped = "some-value"
        }).AsBenzeneMessage();

        var response = await app.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal(BenzeneResultStatus.ValidationError, response.StatusCode);
    }

    [Fact]
    public async Task UnknownTopic_NoSchemaGenerated_FallsThroughToRouter()
    {
        var app = CreateApplication(out var serviceCollection);

        var request = MessageBuilder.Create("no-such-topic", new { id = 1 }).AsBenzeneMessage();

        var response = await app.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal(BenzeneResultStatus.NotFound, response.StatusCode);
    }
}
