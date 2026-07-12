using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddSns"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class SnsRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnsRegistrations"/> class.
    /// </summary>
    public SnsRegistrations()
    {
        Add(".AddSns()", x => x.AddSns());
    }
}
