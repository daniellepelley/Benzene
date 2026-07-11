namespace Benzene.Core.DI;

/// <summary>
/// Validates dependency injection registrations and provides diagnostic information for missing registrations.
/// </summary>
public interface IRegistrationCheck
{
    /// <summary>
    /// Checks if a type is registered in the dependency injection container and provides registration guidance if missing.
    /// </summary>
    /// <param name="typeName">The full name of the type to check.</param>
    /// <returns>A diagnostic message indicating registration status and guidance for missing registrations.</returns>
    string CheckType(string typeName);
}
