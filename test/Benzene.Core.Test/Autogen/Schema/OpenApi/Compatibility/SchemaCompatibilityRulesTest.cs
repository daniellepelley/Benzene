using Benzene.Schema.OpenApi.Compatibility;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi.Compatibility;

/// <summary>
/// The client <em>consumes</em> both responses and events (the service produces them), so an event
/// payload's compatibility rules must match the response side, not the request (producer) side.
/// </summary>
public class SchemaCompatibilityRulesTest
{
    [Theory]
    // Event is consumer-side, exactly like Response.
    [InlineData(SchemaChangeKind.PropertyRemoved, ChangeCompatibility.Breaking)]        // consumer may read the removed field
    [InlineData(SchemaChangeKind.RequiredPropertyAdded, ChangeCompatibility.Compatible)] // consumer tolerates an extra field
    [InlineData(SchemaChangeKind.PropertyBecameRequired, ChangeCompatibility.Compatible)]
    [InlineData(SchemaChangeKind.PropertyBecameOptional, ChangeCompatibility.Warning)]   // consumer may rely on it always being present
    public void DefaultFor_Event_MatchesTheResponseConsumerSide(SchemaChangeKind kind, ChangeCompatibility expected)
    {
        Assert.Equal(expected, SchemaCompatibilityRules.DefaultFor(kind, SchemaDirection.Event));
        // Event and Response are the same consumer side.
        Assert.Equal(SchemaCompatibilityRules.DefaultFor(kind, SchemaDirection.Response),
            SchemaCompatibilityRules.DefaultFor(kind, SchemaDirection.Event));
    }

    [Theory]
    // Regression guard: the request (producer) side is unchanged.
    [InlineData(SchemaChangeKind.PropertyRemoved, ChangeCompatibility.Warning)]
    [InlineData(SchemaChangeKind.RequiredPropertyAdded, ChangeCompatibility.Breaking)]
    [InlineData(SchemaChangeKind.PropertyBecameRequired, ChangeCompatibility.Breaking)]
    [InlineData(SchemaChangeKind.PropertyBecameOptional, ChangeCompatibility.Compatible)]
    public void DefaultFor_Request_IsUnchanged(SchemaChangeKind kind, ChangeCompatibility expected)
    {
        Assert.Equal(expected, SchemaCompatibilityRules.DefaultFor(kind, SchemaDirection.Request));
    }
}
