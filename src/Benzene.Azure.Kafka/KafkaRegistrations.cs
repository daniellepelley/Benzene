using Benzene.Core.DI;

namespace Benzene.Azure.Kafka;

public class KafkaRegistrations : RegistrationsBase
{
    public KafkaRegistrations()
    {
        Add(".AddAzureKafka()", x => x.AddAzureKafka());
    }
}
