using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Core.Versioning;
using Benzene.Core.Versioning.Schemas;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.Test.Core.Versioning;

/// <summary>
/// End-to-end coverage of mechanism B (docs/specification/versioning.md §4): a single handler written
/// against the V2 schema transparently serves a V1 producer - the request is upcast V1 -> V2 before the
/// handler, and the response downcast V2 -> V1 on the way out - wired purely through DI via
/// <c>UsePayloadVersionCasting</c> over the BenzeneMessage transport.
/// </summary>
public class PayloadVersionCastingPipelineTest
{
    // Deliberately no [Message] attribute: registered explicitly below so it isn't picked up by other
    // tests' AppDomain-wide handler scans (which would pollute their generated-spec golden files).
    public class VersioningOrderHandler : IMessageHandler<V2.OrderPayload, V2.OrderPayload>
    {
        public Task<IBenzeneResult<V2.OrderPayload>> HandleAsync(V2.OrderPayload request)
        {
            // Echo the upcast-injected Currency (a value that only exists because the V1->V2 caster ran,
            // never in the incoming V1 JSON) into Id, which survives the downcast back to V1 - so the
            // final V1 response Id proves the whole round trip happened.
            return Task.FromResult(BenzeneResult.Ok(new V2.OrderPayload
            {
                Id = request.Currency,
                Quantity = request.Quantity
            }));
        }
    }

    private static BenzeneMessageApplication BuildApp(out MicrosoftServiceResolverFactory factory)
    {
        var services = new ServiceCollection();
        services
            .AddTransient<ISerializer, JsonSerializer>()
            .AddTransient<JsonSerializer>()
            .UsingBenzene(x => x
                .AddBenzene()
                .AddBenzeneMessage()
                // AddContextItems early so the framework-default mappers are registered before
                // UsePayloadVersionCasting's decorators, which must be the last (winning) registration.
                .AddContextItems()
                .RegisterSchemaCastDefinitions(builder => builder
                    .Add<V1.OrderPayload, V2.OrderPayload>("order", "V1", "V2", f => f.RegisterInitValue(o => o.Currency, "FROM-UPCAST"))
                    .Add<V2.OrderPayload, V1.OrderPayload>("order", "V2", "V1"))
                .RegisterPayloadSchemaVersions(new[]
                {
                    new PayloadSchemaVersions
                    {
                        Topic = "order",
                        FromSchemas = new[] { "V1", "V2" },
                        ToSchemas = new[] { "V1", "V2" }
                    }
                })
                .UsePayloadVersionCasting<BenzeneMessageContext>());

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        // Register the handler explicitly (no [Message] attribute) so it is scoped to this test only.
        pipeline.UseMessageHandlers(new System.Type[0],
            router => router.AddMessageHandler<VersioningOrderHandler, V2.OrderPayload, V2.OrderPayload>("order"));

        factory = new MicrosoftServiceResolverFactory(services);
        return new BenzeneMessageApplication(pipeline.Build());
    }

    [Fact]
    public async Task V1Request_IsUpcastForTheHandler_AndResponseDowncastBackToV1()
    {
        var app = BuildApp(out var factory);

        var request = new BenzeneMessageRequest
        {
            Topic = "order",
            Body = JsonConvert.SerializeObject(new V1.OrderPayload { Id = "order-1", Quantity = 5, CustomerName = "Jo" }),
            Headers = new Dictionary<string, string> { { "benzene-version", "V1" } }
        };

        var response = await app.HandleAsync(request, factory);

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);

        // Response deserializes as V1, and its Id carries the value the V1->V2 upcast caster injected -
        // proving request upcast (caster ran, not plain deserialization) and response downcast both happened.
        var v1 = JsonConvert.DeserializeObject<V1.OrderPayload>(response.Body);
        Assert.Equal("FROM-UPCAST", v1.Id);
        Assert.Equal(5, v1.Quantity);

        // The response is V1-shaped: it carries none of V2's Currency field.
        Assert.DoesNotContain("currency", response.Body, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestWithoutVersionHeader_BypassesCasting()
    {
        var app = BuildApp(out var factory);

        // No version header: the handler runs on whatever the body deserializes to directly (V2), so the
        // upcast caster never injects Currency and Id stays empty.
        var request = new BenzeneMessageRequest
        {
            Topic = "order",
            Body = JsonConvert.SerializeObject(new V2.OrderPayload { Id = "order-2", Quantity = 7 }),
            Headers = new Dictionary<string, string>()
        };

        var response = await app.HandleAsync(request, factory);

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
        var v2 = JsonConvert.DeserializeObject<V2.OrderPayload>(response.Body);
        Assert.Null(v2.Id);
        Assert.Equal(7, v2.Quantity);
    }
}
