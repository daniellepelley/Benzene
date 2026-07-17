using System.Collections.Generic;
using System.Text.Json;
using Benzene.Clients.HealthChecks;
using Benzene.HealthChecks.Core;
using Xunit;

namespace Benzene.Test.Clients;

public class ClientHealthCheckProcessorTest
{
    private static HealthCheckResponse ResponseWithSchemaHash(object hashValue)
    {
        var data = new Dictionary<string, object> { [SchemaHealthCheckConstants.HashCodeKey] = hashValue };
        var schema = (HealthCheckResult)HealthCheckResult.CreateInstance(true, SchemaHealthCheckConstants.Type, data);
        return new HealthCheckResponse(true,
            new Dictionary<string, HealthCheckResult> { [SchemaHealthCheckConstants.Type] = schema });
    }

    private static ClientHashMatch MatchOf(IHealthCheckResponse<HealthCheckResult> response) =>
        (ClientHashMatch)response.HealthChecks[SchemaHealthCheckConstants.Type].Data[SchemaHealthCheckConstants.MatchKey];

    [Fact]
    public void Process_HashesMatch_SetsIsMatchTrue()
    {
        var result = ClientHealthCheckProcessor.Process(ResponseWithSchemaHash("some-hash"), "some-hash");

        var match = MatchOf(result);
        Assert.True(match.IsMatch);
        Assert.Equal("some-hash", match.ServiceHashCode);
        Assert.Equal("some-hash", match.ClientHashCode);
    }

    [Fact]
    public void Process_HashesDiffer_SetsIsMatchFalse()
    {
        var result = ClientHealthCheckProcessor.Process(ResponseWithSchemaHash("service-hash"), "client-hash");

        Assert.False(MatchOf(result).IsMatch);
    }

    [Fact]
    public void Process_HashArrivesAsJsonElementAfterWireRoundTrip_StillReadsIt()
    {
        // After the provider's health response is serialized and deserialized, a Data value is a
        // JsonElement (System.Text.Json), not a boxed string. The old dynamic-based processor broke
        // on this; the hardened one reads it via ToString().
        var jsonElement = JsonDocument.Parse("\"wire-hash\"").RootElement;
        Assert.Equal(JsonValueKind.String, jsonElement.ValueKind);

        var result = ClientHealthCheckProcessor.Process(ResponseWithSchemaHash(jsonElement), "wire-hash");

        var match = MatchOf(result);
        Assert.True(match.IsMatch);
        Assert.Equal("wire-hash", match.ServiceHashCode);
    }

    [Fact]
    public void Process_NoSchemaHealthCheck_PassesResponseThroughUnchanged()
    {
        var response = new HealthCheckResponse(true, new Dictionary<string, HealthCheckResult>());

        var result = ClientHealthCheckProcessor.Process(response, "some-hash");

        Assert.True(result.IsHealthy);
        Assert.Empty(result.HealthChecks);
    }
}
