using Benzene.Core.DI;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddAzureServiceBus"/>, for use by <see cref="RegistrationCheck"/>'s
/// missing-registration diagnostics.
/// </summary>
public class ServiceBusRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusRegistrations"/> class.
    /// </summary>
    public ServiceBusRegistrations()
    {
        Add(".AddAzureServiceBus()", x => x.AddAzureServiceBus());
    }
}
