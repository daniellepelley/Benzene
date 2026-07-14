using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Schema.OpenApi.Compatibility;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi.Compatibility;

public class SchemaCompatibilityComparerTest
{
    private const string Topic = "order:create";

    [Fact]
    public void IdenticalDocuments_AreCompatible_WithNoChanges()
    {
        var doc = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false), ("status", false))));

        var report = new SchemaCompatibilityComparer().Compare(doc, doc);

        Assert.Empty(report.Changes);
        Assert.True(report.IsCompatible);
        Assert.Equal(ChangeCompatibility.Compatible, report.Overall);
    }

    [Fact]
    public void ResponsePropertyAdded_IsCompatible()
    {
        var baseline = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false))));
        var current = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false), ("status", false))));

        var report = new SchemaCompatibilityComparer().Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(SchemaChangeKind.PropertyAdded, change.Kind);
        Assert.Equal(SchemaDirection.Response, change.Direction);
        Assert.Equal(ChangeCompatibility.Compatible, change.Compatibility);
        Assert.True(report.IsCompatible);
    }

    [Fact]
    public void ResponsePropertyRemoved_IsBreaking()
    {
        var baseline = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false), ("status", false))));
        var current = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false))));

        var report = new SchemaCompatibilityComparer().Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(SchemaChangeKind.PropertyRemoved, change.Kind);
        Assert.Equal(SchemaDirection.Response, change.Direction);
        Assert.Equal(ChangeCompatibility.Breaking, change.Compatibility);
        Assert.False(report.IsCompatible);
        Assert.True(report.HasBreakingChanges);
    }

    [Fact]
    public void RequestRequiredPropertyAdded_IsBreaking()
    {
        var baseline = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false))));
        var current = DocOf(Req(Topic, Obj(("id", true), ("customerId", true)), Obj(("id", false))));

        var report = new SchemaCompatibilityComparer().Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(SchemaChangeKind.RequiredPropertyAdded, change.Kind);
        Assert.Equal(SchemaDirection.Request, change.Direction);
        Assert.Equal(ChangeCompatibility.Breaking, change.Compatibility);
        Assert.False(report.IsCompatible);
    }

    [Fact]
    public void RequestOptionalPropertyAdded_IsCompatible()
    {
        var baseline = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false))));
        var current = DocOf(Req(Topic, Obj(("id", true), ("note", false)), Obj(("id", false))));

        var report = new SchemaCompatibilityComparer().Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(SchemaChangeKind.PropertyAdded, change.Kind);
        Assert.Equal(ChangeCompatibility.Compatible, change.Compatibility);
        Assert.True(report.IsCompatible);
    }

    [Fact]
    public void PropertyTypeChanged_IsBreaking()
    {
        var baseline = DocOf(Req(Topic, Obj(("id", true)), WithProp("total", "string")));
        var current = DocOf(Req(Topic, Obj(("id", true)), WithProp("total", "integer")));

        var report = new SchemaCompatibilityComparer().Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(SchemaChangeKind.TypeChanged, change.Kind);
        Assert.Equal(ChangeCompatibility.Breaking, change.Compatibility);
        Assert.False(report.IsCompatible);
    }

    [Fact]
    public void TopicRemoved_IsBreaking()
    {
        var baseline = DocOf(
            Req(Topic, Obj(("id", true)), Obj(("id", false))),
            Req("order:cancel", Obj(("id", true)), Obj(("id", false))));
        var current = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false))));

        var report = new SchemaCompatibilityComparer().Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(SchemaChangeKind.TopicRemoved, change.Kind);
        Assert.Equal(ChangeCompatibility.Breaking, change.Compatibility);
        Assert.False(report.IsCompatible);
    }

    [Fact]
    public void TopicAdded_IsCompatible()
    {
        var baseline = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false))));
        var current = DocOf(
            Req(Topic, Obj(("id", true)), Obj(("id", false))),
            Req("order:cancel", Obj(("id", true)), Obj(("id", false))));

        var report = new SchemaCompatibilityComparer().Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(SchemaChangeKind.TopicAdded, change.Kind);
        Assert.Equal(ChangeCompatibility.Compatible, change.Compatibility);
        Assert.True(report.IsCompatible);
    }

    [Fact]
    public void CustomRule_CanDowngradeBreakingToWarning()
    {
        var baseline = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false), ("status", false))));
        var current = DocOf(Req(Topic, Obj(("id", true)), Obj(("id", false))));

        var rules = SchemaCompatibilityRules.Default()
            .Set(SchemaChangeKind.PropertyRemoved, SchemaDirection.Response, ChangeCompatibility.Warning);

        var report = new SchemaCompatibilityComparer(rules).Compare(baseline, current);

        var change = Assert.Single(report.Changes);
        Assert.Equal(ChangeCompatibility.Warning, change.Compatibility);
        Assert.True(report.IsCompatible);   // no longer breaking
        Assert.True(report.HasWarnings);
    }

    // ---- helpers ----

    private static EventServiceDocument DocOf(params RequestResponse[] requests) =>
        new EventServiceDocument(
            new OpenApiInfo(),
            Array.Empty<OpenApiTag>(),
            requests,
            Array.Empty<Event>(),
            new OpenApiComponents { Schemas = new Dictionary<string, OpenApiSchema>() });

    private static RequestResponse Req(string topic, OpenApiSchema request, OpenApiSchema response) =>
        new RequestResponse { Topic = topic, Version = "", Request = request, Response = response };

    private static OpenApiSchema Obj(params (string Name, bool Required)[] props)
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>()
        };

        foreach (var prop in props)
        {
            schema.Properties[prop.Name] = new OpenApiSchema { Type = "string" };
            if (prop.Required)
            {
                schema.Required.Add(prop.Name);
            }
        }

        return schema;
    }

    private static OpenApiSchema WithProp(string name, string type) =>
        new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> { [name] = new OpenApiSchema { Type = type } },
            Required = new HashSet<string>()
        };
}
