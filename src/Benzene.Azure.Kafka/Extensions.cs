using Benzene.Azure.Core;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Kafka;

public static class Extensions
{
    public static Task HandleKafkaEvents(this IAzureApp source, params KafkaEventData<string>[] eventData)
    {
        return source.HandleAsync(eventData);
    }
}
