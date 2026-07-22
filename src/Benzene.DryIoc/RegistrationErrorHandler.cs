using Benzene.Core.DI;

namespace Benzene.DryIoc;

/// <summary>
/// Enriches a failed service resolution with guidance on the Benzene registration call that would
/// have provided the missing type. The heavy lifting (matching a type name against the registration
/// catalog, and the container-agnostic, throw-safe exception scan) lives in
/// <see cref="RegistrationCheck"/>; this is just the cached, package-local entry point.
/// </summary>
public static class RegistrationErrorHandler
{
    // Building the catalog scans every loaded assembly, so do it once, lazily, and thread-safely.
    private static readonly Lazy<RegistrationCheck> Check =
        new(() => RegistrationCheck.Create(Utils.GetAllTypes().ToArray()));

    /// <summary>Registration guidance for a specific type, or an empty string if it isn't a known Benzene registration.</summary>
    public static string CheckType(Type type) =>
        type.FullName is { } name ? Check.Value.CheckType(name) : string.Empty;

    /// <summary>
    /// Registration guidance derived by scanning a container's resolve exception. Container-agnostic
    /// and never throws (see <see cref="RegistrationCheck.CheckException"/>).
    /// </summary>
    public static string CheckException(Exception exception) => Check.Value.CheckException(exception);

    /// <summary>
    /// Registration guidance for a failed resolve of <paramref name="requestedType"/>, preferring the
    /// requested type itself (reliable on any container) over parsing the exception message. Never
    /// throws (see <see cref="RegistrationCheck.Describe"/>).
    /// </summary>
    public static string Describe(Type requestedType, Exception exception) =>
        Check.Value.Describe(requestedType, exception);
}
