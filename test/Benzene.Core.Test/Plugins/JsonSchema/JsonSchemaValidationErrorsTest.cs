using System.Text.Json;
using Benzene.JsonSchema;
using Json.Schema;
using Xunit;

namespace Benzene.Test.Plugins.JsonSchema;

public class JsonSchemaValidationErrorsTest
{
    private static readonly Json.Schema.JsonSchema Schema = Json.Schema.JsonSchema.FromText(/*lang=json*/ """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "properties": {
            "name": { "type": "string", "maxLength": 5 },
            "lines": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": { "sku": { "type": "string" } },
                "required": [ "sku" ]
              }
            }
          },
          "required": [ "name" ]
        }
        """);

    private static string[] Evaluate(string json)
    {
        using var document = JsonDocument.Parse(json);
        var results = Schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.False(results.IsValid);
        return JsonSchemaValidationErrors.Format(results);
    }

    [Fact]
    public void Format_PropertyFailure_IsScopedToTheInstancePointer()
    {
        var errors = Evaluate("""{ "name": "far-too-long-a-name" }""");

        var error = Assert.Single(errors);
        Assert.StartsWith("/name: ", error);
    }

    [Fact]
    public void Format_RootFailure_HasNoPointerPrefix()
    {
        var errors = Evaluate("""{ }""");

        var error = Assert.Single(errors);
        Assert.False(error.StartsWith("/"), error);
        Assert.Contains("name", error);
    }

    [Fact]
    public void Format_NestedArrayFailure_PointsAtTheFailingElement()
    {
        var errors = Evaluate("""{ "name": "ok", "lines": [ { "sku": "ABC" }, { } ] }""");

        var error = Assert.Single(errors);
        Assert.StartsWith("/lines/1: ", error);
        Assert.Contains("sku", error);
    }

    [Fact]
    public void Format_MultipleFailures_YieldOneMessageEach()
    {
        var errors = Evaluate("""{ "name": "far-too-long-a-name", "lines": [ { } ] }""");

        Assert.Equal(2, errors.Length);
        Assert.Contains(errors, x => x.StartsWith("/name: "));
        Assert.Contains(errors, x => x.StartsWith("/lines/0: "));
    }
}
