using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.BenzeneMessage.TestHelpers;
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

public class JsonSchemaPipelineTest
{
    [Theory]
    [InlineData("foo", BenzeneResultStatus.Ok)]
    [InlineData("foo-bar-foo-bar", BenzeneResultStatus.ValidationError)]
    public async Task ValidationTest(string name, string expectedStatus)
    {
        var jsonSchema = Json.Schema.JsonSchema.FromFile("Plugins/JsonSchema/schema.jsonc");

        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
        serviceCollection
            .AddScoped<IJsonSchemaProvider<BenzeneMessageContext>>(x => new SimpleJsonSchemaProvider<BenzeneMessageContext>(jsonSchema));

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseJsonSchema()
            .UseMessageHandlers();

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload
        {
            Id = 42,
            Name = name,
            Mapped = "some-value"
        }).AsBenzeneMessage();

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task ValidationTest_NoSchema()
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseJsonSchema()
            .UseMessageHandlers();

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload
        {
            Id = 42,
            Name = "foo",
            Mapped = "some-value"
        }).AsBenzeneMessage();

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.NotNull(response);
        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task ValidationTest_NoBody()
    {
        var jsonSchema = Json.Schema.JsonSchema.FromFile("Plugins/JsonSchema/schema.jsonc");

        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
        serviceCollection
             .AddScoped<IJsonSchemaProvider<BenzeneMessageContext>>(x => new SimpleJsonSchemaProvider<BenzeneMessageContext>(jsonSchema));

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseJsonSchema()
            .UseMessageHandlers();

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic)
            .AsBenzeneMessage();

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal(BenzeneResultStatus.ValidationError, response.StatusCode);
    }
}