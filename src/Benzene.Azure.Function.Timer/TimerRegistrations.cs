using Benzene.Core.DI;

namespace Benzene.Azure.Function.Timer;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddAzureTimer"/>, for use by <see cref="RegistrationCheck"/>'s
/// missing-registration diagnostics.
/// </summary>
public class TimerRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimerRegistrations"/> class.
    /// </summary>
    public TimerRegistrations()
    {
        Add(".AddAzureTimer()", x => x.AddAzureTimer());
    }
}
