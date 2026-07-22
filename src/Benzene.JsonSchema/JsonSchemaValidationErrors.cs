using Json.Schema;

namespace Benzene.JsonSchema;

/// <summary>
/// Formats a JSON Schema evaluation failure into the same shape the other validation libraries
/// (<c>Benzene.FluentValidation</c>, <c>Benzene.DataAnnotations</c>) produce: an array of
/// human-readable, property-scoped messages, served as the <c>ValidationError</c> result's payload.
/// </summary>
public static class JsonSchemaValidationErrors
{
    /// <summary>The message used when the request body is absent entirely.</summary>
    public const string MissingBody = "Request body is missing";

    /// <summary>The message used when the request body is not parseable JSON.</summary>
    public const string MalformedBody = "Request body is not valid JSON";

    /// <summary>
    /// Flattens an <see cref="EvaluationResults"/> (evaluated with <see cref="OutputFormat.List"/>)
    /// into one message per failed keyword, prefixed with the JSON Pointer of the failing value
    /// (e.g. <c>"/name: Value is longer than 5 characters"</c>; root-level failures carry no prefix).
    /// </summary>
    /// <param name="results">The failed evaluation.</param>
    /// <returns>De-duplicated, ordered messages; a generic fallback if the evaluation carries no detail.</returns>
    public static string[] Format(EvaluationResults results)
    {
        var messages = results.Details
            .Where(x => !x.IsValid && x.Errors is { Count: > 0 })
            .SelectMany(x => x.Errors!.Values.Select(message => Format(x.InstanceLocation.ToString(), message)))
            .Distinct()
            .ToArray();

        return messages.Length > 0
            ? messages
            : new[] { "Request body does not match the schema" };
    }

    private static string Format(string instanceLocation, string message)
    {
        return string.IsNullOrEmpty(instanceLocation) ? message : $"{instanceLocation}: {message}";
    }
}
