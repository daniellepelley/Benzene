using Benzene.Core.Versioning.Deserializer;
using Benzene.Core.Versioning.Schemas;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;
using V3 = Benzene.Test.Core.Versioning.Schemas.V3;

namespace Benzene.Test.Core.Versioning;

/// <summary>
/// End-to-end coverage of the DI registration path: individually registered schema casters are
/// collected via <c>IServiceResolver.GetServices</c> and expanded into an
/// <see cref="ISchemaCasters"/> singleton, composing multi-step chains where needed.
/// </summary>
public class SchemaCasterRegistrationTest
{
    [Fact]
    public void RegisterPayloadSchemaVersions_ResolvesExpandedSchemaCasters()
    {
        var services = new ServiceCollection();

        services.UsingBenzene(x => x
            .RegisterSchemaCastDefinitions(builder => builder
                .Add<V1.OrderPayload, V2.OrderPayload>("orderCreated", "V1", "V2")
                .Add<V2.OrderPayload, V3.OrderPayload>("orderCreated", "V2", "V3"))
            .RegisterPayloadSchemaVersions(new[]
            {
                new PayloadSchemaVersions
                {
                    Topic = "orderCreated",
                    FromSchemas = new[] { "V1", "V2", "V3" },
                    ToSchemas = new[] { "V3" }
                }
            }));

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        var schemaCasters = scope.GetService<ISchemaCasters>();

        var composite = (ISchemaCaster<V1.OrderPayload, V3.OrderPayload>)schemaCasters
            .GetSchemaCaster("V1", "V3", "orderCreated");

        var result = composite.Caster.Cast(new V1.OrderPayload { Id = "order-1", Quantity = 5 });

        Assert.Equal("order-1", result.Id);
        Assert.Equal(5, result.Quantity);
    }
}
