using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddKafka"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class KafkaRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaRegistrations"/> class.
    /// </summary>
    public KafkaRegistrations()
    {
        Add(".AddKafka()", x => x.AddKafka());
    }
}
