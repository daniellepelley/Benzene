using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Schema.OpenApi.Compatibility;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi.Compatibility;

// The CI-gate side of contract testing: EnsureBackwardCompatible turns the SchemaCompatibilityComparer
// into a pass/fail check a service drops into its own test suite. It builds the "current" contract
// from the service's live handlers and compares it against a committed baseline, failing the build
// on a breaking change while allowing additive ones.
public class SchemaCompatibilityGateTest
{
    public class CreateOrder { public string Id { get; set; } = ""; }

    // Response v1 has Id + Status.
    public class OrderV1 { public string Id { get; set; } = ""; public string Status { get; set; } = ""; }

    // Additive: v1 + an optional Notes field.
    public class OrderV1Plus { public string Id { get; set; } = ""; public string Status { get; set; } = ""; public string Notes { get; set; } = ""; }

    // Breaking: Status removed - a consumer reading Status breaks.
    public class OrderV2 { public string Id { get; set; } = ""; }

    private static EventServiceDocument Contract(System.Type responseType) =>
        new IMessageHandlerDefinition[]
        {
            MessageHandlerDefinition.CreateInstance("order:create", typeof(CreateOrder), responseType),
        }.ToEventServiceDocument();

    [Fact]
    public void EnsureBackwardCompatible_AdditiveChange_Passes()
    {
        var baseline = Contract(typeof(OrderV1));
        var current = Contract(typeof(OrderV1Plus));

        var report = SchemaCompatibility.EnsureBackwardCompatible(baseline, current);

        Assert.False(report.HasBreakingChanges);
    }

    [Fact]
    public void EnsureBackwardCompatible_BreakingChange_Throws()
    {
        var baseline = Contract(typeof(OrderV1));
        var current = Contract(typeof(OrderV2)); // Status removed from the response

        var exception = Assert.Throws<SchemaCompatibilityException>(
            () => SchemaCompatibility.EnsureBackwardCompatible(baseline, current));

        Assert.True(exception.Report.HasBreakingChanges);
        Assert.Contains(exception.Report.BreakingChanges, x => x.Kind == SchemaChangeKind.PropertyRemoved);
        Assert.Contains("breaking", exception.Message);
    }

    [Fact]
    public void EnsureBackwardCompatible_IdenticalContract_Passes()
    {
        var contract = Contract(typeof(OrderV1));

        var report = SchemaCompatibility.EnsureBackwardCompatible(contract, contract);

        Assert.Empty(report.Changes);
    }

    [Fact]
    public void EnsureBackwardCompatible_BaselineLoadedFromJson_Works()
    {
        // The realistic workflow: the baseline is a committed spec.json; the gate deserializes it and
        // compares against the current contract built from handlers.
        var baselineJson = Contract(typeof(OrderV1)).SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

        // Additive current spec round-trips through the JSON baseline and passes.
        SchemaCompatibility.EnsureBackwardCompatible(baselineJson, Contract(typeof(OrderV1Plus)));

        // Breaking current spec against the JSON baseline throws.
        Assert.Throws<SchemaCompatibilityException>(
            () => SchemaCompatibility.EnsureBackwardCompatible(baselineJson, Contract(typeof(OrderV2))));
    }
}
