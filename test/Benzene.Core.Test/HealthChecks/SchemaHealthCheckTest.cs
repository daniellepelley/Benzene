using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Clients.HealthChecks;
using Benzene.CodeGen.Core;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.Schema;
using Moq;
using Xunit;

namespace Benzene.Test.HealthChecks;

public class SchemaHealthCheckTest
{
    // Simple request/response POCOs so GenerateHash has real schemas to hash.
    public class CreateThing { public string Name { get; set; } = ""; }
    public class ThingCreated { public string Id { get; set; } = ""; }

    private static IMessageHandlerDefinition[] Handlers() =>
    [
        MessageHandlerDefinition.CreateInstance("thing:create", typeof(CreateThing), typeof(ThingCreated)),
    ];

    private static Mock<IMessageHandlerDefinitionLookUp> Lookup(IMessageHandlerDefinition[] handlers)
    {
        var lookup = new Mock<IMessageHandlerDefinitionLookUp>();
        lookup.Setup(x => x.GetAllHandlers()).Returns(handlers);
        return lookup;
    }

    [Fact]
    public async Task ExecuteAsync_PublishesTheCanonicalContractHash()
    {
        var handlers = Handlers();

        var result = await new SchemaHealthCheck(Lookup(handlers).Object).ExecuteAsync();

        Assert.Equal(SchemaHealthCheckConstants.Type, result.Type);
        // The provider publishes exactly the hash CodeGen bakes into a generated client, so the two
        // are directly comparable.
        Assert.Equal(CodeGenHelpers.GenerateHash(handlers),
            result.Data[SchemaHealthCheckConstants.HashCodeKey]?.ToString());
    }

    [Fact]
    public async Task EndToEnd_ProviderHashMatchesClientBakedHash_ReportsMatch()
    {
        var handlers = Handlers();
        // A consumer's generated client bakes in CodeGenHelpers.GenerateHash(...) at generation time.
        var clientBakedHash = CodeGenHelpers.GenerateHash(handlers);

        var schemaResult = (HealthCheckResult)await new SchemaHealthCheck(Lookup(handlers).Object).ExecuteAsync();
        var providerResponse = new HealthCheckResponse(true,
            new Dictionary<string, HealthCheckResult> { [SchemaHealthCheckConstants.Type] = schemaResult });

        var processed = ClientHealthCheckProcessor.Process(providerResponse, clientBakedHash);

        var match = (ClientHashMatch)processed.HealthChecks[SchemaHealthCheckConstants.Type]
            .Data[SchemaHealthCheckConstants.MatchKey];
        Assert.True(match.IsMatch);
    }

    [Fact]
    public async Task EndToEnd_ProviderContractChanged_ReportsDrift()
    {
        // The client was generated against the original contract...
        var clientBakedHash = CodeGenHelpers.GenerateHash(Handlers());

        // ...but the provider now exposes a different contract (an extra handler).
        var changedHandlers = new IMessageHandlerDefinition[]
        {
            MessageHandlerDefinition.CreateInstance("thing:create", typeof(CreateThing), typeof(ThingCreated)),
            MessageHandlerDefinition.CreateInstance("thing:delete", typeof(CreateThing), typeof(ThingCreated)),
        };
        var schemaResult = (HealthCheckResult)await new SchemaHealthCheck(Lookup(changedHandlers).Object).ExecuteAsync();
        var providerResponse = new HealthCheckResponse(true,
            new Dictionary<string, HealthCheckResult> { [SchemaHealthCheckConstants.Type] = schemaResult });

        var processed = ClientHealthCheckProcessor.Process(providerResponse, clientBakedHash);

        var match = (ClientHashMatch)processed.HealthChecks[SchemaHealthCheckConstants.Type]
            .Data[SchemaHealthCheckConstants.MatchKey];
        Assert.False(match.IsMatch);
    }
}
