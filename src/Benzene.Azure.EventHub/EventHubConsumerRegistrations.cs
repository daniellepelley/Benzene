using Benzene.Core.DI;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddEventHubConsumer"/>, for use by
/// <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class EventHubConsumerRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubConsumerRegistrations"/> class.
    /// </summary>
    public EventHubConsumerRegistrations()
    {
        Add(".AddEventHubConsumer()", x => x.AddEventHubConsumer());
    }
}
