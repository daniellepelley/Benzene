using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Kafka.Core.KafkaMessage;
using Confluent.Kafka;

namespace Benzene.Kafka.Core;

public class BenzeneKafkaWorker : IBenzeneWorker, IDisposable
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private IConsumer<Ignore, string>? _consumer;
    private readonly KafkaApplication<Ignore, string> _kafkaApplication;
    private readonly BenzeneKafkaConfig _benzeneKafkaConfig;

    public BenzeneKafkaWorker(IServiceResolverFactory serviceResolverFactory,
        KafkaApplication<Ignore, string> kafkaApplication, BenzeneKafkaConfig benzeneKafkaConfig)
    {
        _benzeneKafkaConfig = benzeneKafkaConfig;
        _kafkaApplication = kafkaApplication;
        _serviceResolverFactory = serviceResolverFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            _consumer = new ConsumerBuilder<Ignore, string>(_benzeneKafkaConfig.ConsumerConfig).Build();
            _consumer.Subscribe(_benzeneKafkaConfig.Topics);

            var semaphore = new SemaphoreSlim(_benzeneKafkaConfig.ConcurrentRequests);
            
            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    
                    try
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        var consumeResult = _consumer.Consume(cancellationToken);
                        _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory.CreateScope())
                            .ContinueWith(_ => semaphore.Release());
                    }
                    catch (ConsumeException e)
                    {
                        semaphore.Release();
                        Console.WriteLine($"Error occured: {e.Error.Reason}");
                    }
                    catch (Exception ex)
                    {
                        semaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _consumer.Close();
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer.Close();
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }

}