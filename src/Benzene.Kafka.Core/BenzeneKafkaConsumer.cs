using Benzene.Abstractions.DI;
using Benzene.Kafka.Core.KafkaMessage;
using Confluent.Kafka;

namespace Benzene.Kafka.Core;

public class BenzeneKafkaConsumer : IDisposable
{
    private readonly ConsumerConfig _consumerConfig;
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private IConsumer<Ignore, string>? _consumer;
    private readonly KafkaApplication<Ignore, string> _kafkaApplication;

    public BenzeneKafkaConsumer(IServiceResolverFactory serviceResolverFactory,
        KafkaApplication<Ignore, string> kafkaApplication, ConsumerConfig consumerConfig)
    {
        _kafkaApplication = kafkaApplication;
        _serviceResolverFactory = serviceResolverFactory;
        _consumerConfig = consumerConfig;
    }

    public async Task Start(IEnumerable<string> topics, CancellationToken token)
    {
        _consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
        _consumer.Subscribe(topics);

            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        var consumeResult = _consumer.Consume(token);
                        await _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory.CreateScope());
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Error occured: {e.Error.Reason}");
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _consumer.Close();
            }
    }

    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }
}