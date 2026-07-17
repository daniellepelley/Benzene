using System.Threading.Tasks;
using Benzene.SchemaRegistry.Core;
using Xunit;

namespace Benzene.Test.SchemaRegistry;

public class InMemorySchemaRegistryClientTest
{
    private static SchemaDefinition Schema(string subject, string body)
        => new(subject, body, SchemaFormat.Avro);

    [Fact]
    public async Task Register_AssignsId_AndIsIdempotentForIdenticalSchema()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.None);

        var id1 = await registry.RegisterAsync(Schema("orders-value", "{\"v\":1}"));
        var id2 = await registry.RegisterAsync(Schema("orders-value", "{\"v\":1}")); // identical

        Assert.Equal(id1, id2);
    }

    [Fact]
    public async Task Register_DistinctSubjects_GetDistinctIds()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.None);

        var a = await registry.RegisterAsync(Schema("a-value", "{\"v\":1}"));
        var b = await registry.RegisterAsync(Schema("b-value", "{\"v\":1}"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task GetById_ReturnsRegisteredSchema_NullForUnknown()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.None);
        var id = await registry.RegisterAsync(Schema("orders-value", "{\"v\":1}"));

        var found = await registry.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Equal("orders-value", found!.Subject);
        Assert.Equal(1, found.Version);

        Assert.Null(await registry.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetLatest_ReturnsHighestVersion()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.None);
        await registry.RegisterAsync(Schema("orders-value", "{\"v\":1}"));
        await registry.RegisterAsync(Schema("orders-value", "{\"v\":2}"));

        var latest = await registry.GetLatestAsync("orders-value");

        Assert.Equal(2, latest!.Version);
        Assert.Equal("{\"v\":2}", latest.Schema);
    }

    [Fact]
    public async Task Register_IncompatibleUnderBackward_Throws_ButNoneAllows()
    {
        // Default textual checker: under Backward, a changed schema for an existing subject is
        // rejected; under None it's accepted.
        var strict = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.Backward);
        await strict.RegisterAsync(Schema("orders-value", "{\"v\":1}"));

        Assert.False(await strict.IsCompatibleAsync(Schema("orders-value", "{\"v\":2}")));
        await Assert.ThrowsAsync<SchemaIncompatibleException>(
            () => strict.RegisterAsync(Schema("orders-value", "{\"v\":2}")));

        var lax = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.None);
        await lax.RegisterAsync(Schema("orders-value", "{\"v\":1}"));
        await lax.RegisterAsync(Schema("orders-value", "{\"v\":2}")); // allowed
    }

    [Fact]
    public async Task FirstSchemaForSubject_IsAlwaysCompatible()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.Full);

        Assert.True(await registry.IsCompatibleAsync(Schema("new-value", "{\"v\":1}")));
    }
}
