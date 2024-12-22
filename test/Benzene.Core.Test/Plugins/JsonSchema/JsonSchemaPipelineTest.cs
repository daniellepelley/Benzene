using System.Threading.Tasks;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.BenzeneMessage.TestHelpers;
using Benzene.Core.DI;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.JsonSchema;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Tools;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Plugins.JsonSchema;

public class JsonSchemaPipelineTest
{
    [Theory]
    [InlineData("foo", ServiceResultStatus.Ok)]
    [InlineData("foo-bar-foo-bar", ServiceResultStatus.ValidationError)]
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

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverAdapter(serviceCollection.BuildServiceProvider()));

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

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverAdapter(serviceCollection.BuildServiceProvider()));

        Assert.NotNull(response);
        Assert.Equal(ServiceResultStatus.Ok, response.StatusCode);
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

        var response = await aws.HandleAsync(request, new MicrosoftServiceResolverAdapter(serviceCollection.BuildServiceProvider()));

        Assert.Equal(ServiceResultStatus.ValidationError, response.StatusCode);
    }
}