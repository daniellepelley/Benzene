using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Validation;
using Benzene.Schema.OpenApi;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class OpenApiValidationSchemaBuilderTest
{
    private sealed class FakeValidationSchema : IValidationSchema
    {
        public FakeValidationSchema(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }
    }

    private sealed class FakeValidationSchemaBuilder : IValidationSchemaBuilder
    {
        private readonly IDictionary<string, IValidationSchema[]> _schemas;
        public FakeValidationSchemaBuilder(IDictionary<string, IValidationSchema[]> schemas) => _schemas = schemas;
        public IDictionary<string, IValidationSchema[]> GetValidationSchemas(Type type) => _schemas;
    }

    [Fact]
    public void AddSchema_ValidationRuleForAMemberNotInTheSchema_DoesNotThrow()
    {
        // A RuleFor on a member that isn't a serialized schema property (e.g. a [JsonIgnore] property,
        // or a rule keyed on a non-property member) used to hit `schema.Properties[key]` and throw
        // KeyNotFoundException, failing the ENTIRE spec build (a 500 on the spec endpoint).
        var validation = new FakeValidationSchemaBuilder(new Dictionary<string, IValidationSchema[]>
        {
            { "GhostProperty", new IValidationSchema[] { new FakeValidationSchema(ValidationConstants.NotEmpty, "req") } }
        });
        var builder = new OpenApiValidationSchemaBuilder(new SchemaBuilder(), validation);

        var exception = Record.Exception(() => builder.AddSchema(typeof(ExampleRequestPayload)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddSchema_GenericType_DecoratesTheArgPrefixedSchemaId()
    {
        // A generic wrapper is catalogued under Swashbuckle's arg-prefixed id
        // (MessageWrapper<GetUserMessage> => GetUserMessageMessageWrapper); the lookup used raw
        // type.Name ("MessageWrapper") and silently skipped decoration for every generic type.
        var validation = new FakeValidationSchemaBuilder(new Dictionary<string, IValidationSchema[]>
        {
            { "Message", new IValidationSchema[] { new FakeValidationSchema(ValidationConstants.NotEmpty, "required") } }
        });
        var builder = new OpenApiValidationSchemaBuilder(new SchemaBuilder(), validation);

        builder.AddSchema(typeof(Benzene.Test.Autogen.CodeGen.Model.MessageWrapper<Benzene.Test.Autogen.CodeGen.Model.GetUserMessage>));
        var schema = builder.Build()["GetUserMessageMessageWrapper"];

        Assert.Contains("Message", schema.Required);
        Assert.Equal("required", schema.Properties["Message"].Description);
    }

    [Fact]
    public void AddSchema_ValidationRuleForARealProperty_StillDecoratesTheSchema()
    {
        // The happy path must be unchanged: a rule keyed on a real property still applies.
        var validation = new FakeValidationSchemaBuilder(new Dictionary<string, IValidationSchema[]>
        {
            { "Name", new IValidationSchema[] { new FakeValidationSchema(ValidationConstants.NotEmpty, "required") } }
        });
        var builder = new OpenApiValidationSchemaBuilder(new SchemaBuilder(), validation);

        builder.AddSchema(typeof(ExampleRequestPayload));
        var schema = builder.Build()["ExampleRequestPayload"];

        Assert.Contains("Name", schema.Required);
        Assert.Equal("required", schema.Properties["Name"].Description);
    }

    [Fact]
    public void AddSchema_MixOfRealAndGhostKeys_AppliesRealAndSkipsGhostWithoutThrowing()
    {
        var validation = new FakeValidationSchemaBuilder(new Dictionary<string, IValidationSchema[]>
        {
            { "Name", new IValidationSchema[] { new FakeValidationSchema(ValidationConstants.NotEmpty, "required") } },
            { "GhostProperty", new IValidationSchema[] { new FakeValidationSchema(ValidationConstants.NotEmpty, "req") } }
        });
        var builder = new OpenApiValidationSchemaBuilder(new SchemaBuilder(), validation);

        builder.AddSchema(typeof(ExampleRequestPayload));
        var schema = builder.Build()["ExampleRequestPayload"];

        Assert.Contains("Name", schema.Required);
        Assert.DoesNotContain("GhostProperty", schema.Required);
    }
}
