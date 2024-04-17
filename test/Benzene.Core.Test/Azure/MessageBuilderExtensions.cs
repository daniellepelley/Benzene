using System;
using Azure.Messaging.EventHubs;
using Benzene.Tools;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Test.Azure;

public static class MessageBuilderExtensions
{
    public static EventData AsEventHubDirectMessage(this MessageBuilder source)
    {
        return new EventData
        {
            EventBody = new BinaryData(source.AsDirectMessage())
        };
    }

    public static KafkaEventData<string> AsAzureKafkaEvent(this MessageBuilder source)
    {
        return new KafkaEventData<string>
        {
            Topic = source.Topic,
            Value = new Benzene.Core.Serialization.JsonSerializer().Serialize(source.Message)
        };
    }

}
