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

public class SuppliedJsonSchemaProviderTest
{
    private const string StrictSchemaJson = /*lang=json*/ """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "properties": {
            "name": { "type": "string", "maxLength": 5 }
          },
          "required": [ "name" ]
        }
        """;

    private static async Task<string> SendAsync(SuppliedJsonSchemaCatalog catalog, string name)
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x
            .AddBenzeneMessage()
            // Registered in ConfigureServices - must win over UseJsonSchema's default provider.
            .AddSuppliedJsonSchemas(catalog));

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(
            new MicrosoftBenzeneServiceContainer(serviceCollection));

        pipeline
            .UseJsonSchema()
            .UseMessageHandlers();

        var app = new BenzeneMessageApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload
        {
            Id = 42,
            Name = name,
            Mapped = "some-value"
        }).AsBenzeneMessage();

        var response = await app.HandleAsync(request,
            new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));
        return response.StatusCode;
    }

    [Theory]
    [InlineData("foo", BenzeneResultStatus.Ok)]
    [InlineData("foo-bar-foo-bar", BenzeneResultStatus.ValidationError)]
    public async Task MappedRequestType_ValidatesAgainstSuppliedSchema(string name, string expectedStatus)
    {
        var catalog = new SuppliedJsonSchemaCatalog()
            .AddJson(typeof(ExampleRequestPayload), StrictSchemaJson);

        Assert.Equal(expectedStatus, await SendAsync(catalog, name));
    }

    [Fact]
    public async Task UnmappedRequestType_FallsBackToGeneratedSchema()
    {
        // Empty catalog: the provider falls back to the default generated-from-type schema,
        // which this valid payload passes.
        Assert.Equal(BenzeneResultStatus.Ok, await SendAsync(new SuppliedJsonSchemaCatalog(), "foo"));
    }
}
