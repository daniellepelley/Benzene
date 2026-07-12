using Benzene.Core.DI;

namespace Benzene.Azure.AspNet;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddAspNet"/>, for use by <see cref="RegistrationCheck"/>'s
/// missing-registration diagnostics.
/// </summary>
public class AspNetRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetRegistrations"/> class.
    /// </summary>
    public AspNetRegistrations()
    {
        Add(".AddAspNet()", x => x.AddAspNet());
    }
}
