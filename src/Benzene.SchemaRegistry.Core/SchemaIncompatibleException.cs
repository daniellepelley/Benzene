namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// Thrown when registering a schema that isn't compatible with the subject's existing versions under
/// the configured compatibility mode — the registry's way of stopping a breaking contract change at
/// the source.
/// </summary>
public class SchemaIncompatibleException : Exception
{
    /// <summary>Initializes the exception for the given subject.</summary>
    /// <param name="subject">The subject the incompatible schema targeted.</param>
    public SchemaIncompatibleException(string subject)
        : base($"Schema for subject '{subject}' is not compatible with the subject's latest version.")
    {
        Subject = subject;
    }

    /// <summary>Gets the subject the incompatible schema targeted.</summary>
    public string Subject { get; }
}
