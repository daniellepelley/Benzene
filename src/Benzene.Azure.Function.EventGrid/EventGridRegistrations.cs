using Benzene.Core.DI;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddAzureEventGrid"/>, for use by <see cref="RegistrationCheck"/>'s
/// missing-registration diagnostics.
/// </summary>
public class EventGridRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridRegistrations"/> class.
    /// </summary>
    public EventGridRegistrations()
    {
        Add(".AddAzureEventGrid()", x => x.AddAzureEventGrid());
    }
}
