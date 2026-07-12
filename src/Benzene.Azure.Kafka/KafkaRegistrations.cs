using Benzene.Core.DI;

namespace Benzene.Azure.Kafka;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddAzureKafka"/>, for use by <see cref="RegistrationCheck"/>'s
/// missing-registration diagnostics.
/// </summary>
public class KafkaRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaRegistrations"/> class.
    /// </summary>
    public KafkaRegistrations()
    {
        Add(".AddAzureKafka()", x => x.AddAzureKafka());
    }
}
