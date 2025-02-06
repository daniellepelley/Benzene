using Benzene.Core.DI;

namespace Benzene.Aws.Kafka;

public class KafkaRegistrations : RegistrationsBase
{
    public KafkaRegistrations()
    {
        Add(".AddKafka()", x => x.AddKafka());
    }
}
