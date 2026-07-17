namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// A schema to register or check: the subject it belongs to (the registry's namespace key, by
/// convention <c>&lt;topic&gt;-value</c> or <c>&lt;topic&gt;-key</c> for Kafka), the schema text, and
/// its format.
/// </summary>
public class SchemaDefinition
{
    /// <summary>Initializes a schema definition.</summary>
    /// <param name="subject">The registry subject.</param>
    /// <param name="schema">The schema text (e.g. an Avro <c>.avsc</c> document).</param>
    /// <param name="format">The schema format. Defaults to <see cref="SchemaFormat.Avro"/>.</param>
    public SchemaDefinition(string subject, string schema, SchemaFormat format = SchemaFormat.Avro)
    {
        Subject = subject;
        Schema = schema;
        Format = format;
    }

    /// <summary>Gets the registry subject this schema belongs to.</summary>
    public string Subject { get; }

    /// <summary>Gets the schema text.</summary>
    public string Schema { get; }

    /// <summary>Gets the schema format.</summary>
    public SchemaFormat Format { get; }
}
