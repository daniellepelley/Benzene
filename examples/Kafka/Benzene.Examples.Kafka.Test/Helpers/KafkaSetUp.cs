using Benzene.Examples.App.Handlers;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Benzene.Examples.Kafka.Test.Helpers;

public static class KafkaSetUp 
{
    private static KafkaSender _sender;

    public static async Task SendAsync(string topic, object message)
    {
        await _sender.SendAsync(topic, message);
    }
    
    public static async Task SetUpAsync()
    {
        ProducerConfig producerConfig = new()
        {
            BootstrapServers = "localhost:29092",
            SaslMechanism = SaslMechanism.Plain,
            SecurityProtocol = SecurityProtocol.Plaintext,
        };

        await DeleteAllTopics(producerConfig);
        await CreateTopics(producerConfig, MessageTopicNames.OrderCreate, MessageTopicNames.OrderDelete, MessageTopicNames.OrderGet, MessageTopicNames.OrderGetAll, MessageTopicNames.OrderUpdate);
        
        _sender = new KafkaSender(producerConfig);
    }

    public static async Task TearDownAsync()
    {
        ProducerConfig producerConfig = new()
        {
            BootstrapServers = "localhost:29092",
            SaslMechanism = SaslMechanism.Plain,
            SecurityProtocol = SecurityProtocol.Plaintext,
        };

        await DeleteAllTopics(producerConfig);
        _sender.Dispose();
        _sender = null;
    }
    
    private static async Task DeleteAllTopics(ProducerConfig producerConfig)
    {
        try
        {
            var adminClient = new AdminClientBuilder(producerConfig).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicNames = metadata.Topics.Select(a => a.Topic).ToArray();

            if (topicNames.Any())
            {
                await adminClient.DeleteTopicsAsync(topicNames);
            }
        }
        catch (CreateTopicsException createTopicsException)
        {
            Console.WriteLine(createTopicsException);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
    
    private static async Task CreateTopics(ProducerConfig producerConfig, params string[] topicNames)
    {
        try
        {
            var adminClient = new AdminClientBuilder(producerConfig).Build();
            var topicSpecifications = topicNames.Select(topicName => new TopicSpecification
            {
                Name = topicName
            }).ToArray();
            await adminClient.CreateTopicsAsync(topicSpecifications);
        }
        catch (CreateTopicsException createTopicsException)
        {
            Console.WriteLine(createTopicsException);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
}