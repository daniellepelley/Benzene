namespace Benzene.SchemaRegistry.Core;

/// <summary>A schema as stored in the registry: its global id and per-subject version, plus the definition.</summary>
public class RegisteredSchema
{
    /// <summary>Initializes a registered schema.</summary>
    /// <param name="id">The registry-wide schema id (what the Confluent wire format embeds).</param>
    /// <param name="subject">The subject this version belongs to.</param>
    /// <param name="version">The 1-based version within the subject.</param>
    /// <param name="schema">The schema text.</param>
    /// <param name="format">The schema format.</param>
    public RegisteredSchema(int id, string subject, int version, string schema, SchemaFormat format)
    {
        Id = id;
        Subject = subject;
        Version = version;
        Schema = schema;
        Format = format;
    }

    /// <summary>Gets the registry-wide schema id.</summary>
    public int Id { get; }

    /// <summary>Gets the subject this version belongs to.</summary>
    public string Subject { get; }

    /// <summary>Gets the 1-based version within the subject.</summary>
    public int Version { get; }

    /// <summary>Gets the schema text.</summary>
    public string Schema { get; }

    /// <summary>Gets the schema format.</summary>
    public SchemaFormat Format { get; }
}
