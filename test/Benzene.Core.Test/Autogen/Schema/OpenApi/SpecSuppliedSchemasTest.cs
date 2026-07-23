using System.Threading.Tasks;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

/// <summary>
/// End-to-end proof of the ISchemaBuilder DI seam: a service that registers supplied (hand-authored)
/// schemas serves them from its live spec topic instead of reflecting the CLR types.
/// </summary>
public class SpecSuppliedSchemasTest
{
    private const string SuppliedExampleJson = /*lang=json*/ """
        {
          "type": "object",
          "properties": {
            "suppliedMarker": { "type": "string" },
            "name": { "type": "string", "maxLength": 5 }
          }
        }
        """;

    private static AwsLambdaBenzeneTestHost CreateHost()
    {
        var catalog = new SuppliedSchemaCatalog()
            .AddJson(typeof(ExampleRequestPayload), "SuppliedExample", SuppliedExampleJson);

        return new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .UsingBenzene(b => b
                    .AddBenzene()
                    .AddBenzeneMessage()
                    .AddHttpMessageHandlers()
                    .AddSuppliedSchemas(catalog)
                    .SetApplicationInfo("Example App", "1.0", "Stuff")
                ))
            .Configure(app =>
            {
                app.UseBenzeneMessage(x => x
                    .UseSpec()
                    .UseMessageHandlers()
                );
            })
            .BuildHost();
    }

    [Fact]
    public void OpenApiBuilder_UsesSuppliedSchemaForMappedRequestType()
    {
        // Direct builder test with a POST endpoint - a GET carries no request body, so the request
        // schema only flows into the OpenAPI document for body-bearing methods.
        var catalog = new SuppliedSchemaCatalog()
            .AddJson(typeof(ExampleRequestPayload), "SuppliedExample", SuppliedExampleJson);
        var builder = new global::Benzene.Schema.OpenApi.OpenApi.OpenApiDocumentBuilder(
            new SuppliedSchemaBuilder(catalog, new SchemaBuilder()));

        builder.AddHttpEndpointDefinitions(
            new IHttpEndpointDefinition[] { new HttpEndpointDefinition("POST", "/example", Defaults.Topic) },
            new[]
            {
                MessageHandlerDefinition.CreateInstance(Defaults.Topic, typeof(ExampleRequestPayload),
                    typeof(ExampleResponsePayload), typeof(ExampleMessageHandler)),
            });
        var document = builder.Build();

        Assert.True(document.Components.Schemas.ContainsKey("SuppliedExample"));
        Assert.True(document.Components.Schemas["SuppliedExample"].Properties.ContainsKey("suppliedMarker"));
        // The mapped CLR type is no longer reflected into the document.
        Assert.False(document.Components.Schemas.ContainsKey(nameof(ExampleRequestPayload)));
    }

    [Fact]
    public async Task BenzeneSpec_UsesSuppliedSchemaForMappedType()
    {
        var host = CreateHost();

        var response = await host.SendBenzeneMessageAsync(
            MessageBuilder.Create("spec", new SpecRequest("benzene", "json")));

        Assert.Contains("suppliedMarker", response.Body);
        Assert.Contains("SuppliedExample", response.Body);
    }
}
