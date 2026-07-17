namespace Benzene.Configuration.Core;

/// <summary>
/// Thrown when one or more required secrets are absent or blank. Carries the full list of missing
/// names so a misconfigured deployment fails fast at startup with everything that is wrong at once,
/// not one redeploy at a time.
/// </summary>
public class MissingSecretException : Exception
{
    /// <summary>Initializes the exception for the given missing names.</summary>
    /// <param name="missingNames">The required names that were absent or blank.</param>
    public MissingSecretException(IReadOnlyCollection<string> missingNames)
        : base($"Required secret(s) missing or empty: {string.Join(", ", missingNames)}")
    {
        MissingNames = missingNames;
    }

    /// <summary>Gets the required names that were absent or blank.</summary>
    public IReadOnlyCollection<string> MissingNames { get; }
}
