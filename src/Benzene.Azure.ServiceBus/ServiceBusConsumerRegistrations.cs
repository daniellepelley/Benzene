using Benzene.Core.DI;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddServiceBusConsumer"/>, for use by
/// <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class ServiceBusConsumerRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusConsumerRegistrations"/> class.
    /// </summary>
    public ServiceBusConsumerRegistrations()
    {
        Add(".AddServiceBusConsumer()", x => x.AddServiceBusConsumer());
    }
}
