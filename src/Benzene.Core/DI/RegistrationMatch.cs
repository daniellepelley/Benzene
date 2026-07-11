namespace Benzene.Core.DI;

/// <summary>
/// Represents a matched dependency injection registration for diagnostic purposes.
/// </summary>
public class RegistrationMatch
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationMatch"/> class.
    /// </summary>
    /// <param name="type">The type that is registered.</param>
    /// <param name="method">The registration method name.</param>
    /// <param name="package">The package containing the registration.</param>
    public RegistrationMatch(string type, string method, string package)
    {
        Type = type;
        Method = method;
        Package = package;
    }

    /// <summary>
    /// Gets the type that is registered.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the registration method name.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the package containing the registration.
    /// </summary>
    public string Package { get; }
}
