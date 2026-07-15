using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.SelfHost;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Benzene.Kafka.Core;

public class BenzeneKafkaWorker<TKey, TValue> : IBenzeneWorker, IDisposable
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly KafkaApplication<TKey, TValue> _kafkaApplication;
    private readonly BenzeneKafkaConfig _benzeneKafkaConfig;
    private readonly ILogger<BenzeneKafkaWorker<TKey, TValue>> _logger;
    private readonly CancellationTokenSource _stoppingCts = new();
    private IConsumer<TKey, TValue>? _consumer;
    private Task? _runTask;
    private CancellationTokenSource? _linkedCts;

    public BenzeneKafkaWorker(IServiceResolverFactory serviceResolverFactory,
        KafkaApplication<TKey, TValue> kafkaApplication, BenzeneKafkaConfig benzeneKafkaConfig,
        ILogger<BenzeneKafkaWorker<TKey, TValue>> logger)
    {
        _benzeneKafkaConfig = benzeneKafkaConfig;
        _kafkaApplication = kafkaApplication;
        _serviceResolverFactory = serviceResolverFactory;
        _logger = logger;
    }

    /// <summary>
    /// Starts the consume loop on a background task and returns immediately - it does not wait for
    /// the loop to run to completion. Use <see cref="StopAsync"/> to signal shutdown and wait for
    /// in-flight messages to drain.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingCts.Token);
        var runToken = _linkedCts.Token;

        _runTask = Task.Run(async () =>
        {
            _consumer = new ConsumerBuilder<TKey, TValue>(_benzeneKafkaConfig.ConsumerConfig).Build();
            _consumer.Subscribe(_benzeneKafkaConfig.Topics);

            Func<ConsumeResult<TKey, TValue>, int>? keySelector = _benzeneKafkaConfig.PreserveOrderPerPartition
                ? consumeResult => consumeResult.Partition.Value
                : null;

            var dispatcher = new BoundedConcurrentDispatcher<ConsumeResult<TKey, TValue>>(
                _benzeneKafkaConfig.ConcurrentRequests,
                (consumeResult, _) => _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory),
                _logger,
                keySelector);

            try
            {
                while (!runToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(runToken);
                        await dispatcher.EnqueueAsync(consumeResult, runToken);
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError(e, "Kafka consume error: {Reason}", e.Error.Reason);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown - fall through to drain and close below.
            }

            await dispatcher.DrainAsync(_benzeneKafkaConfig.DrainTimeout);
            _consumer.Close();
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Signals the consume loop to stop, then waits for it to drain in-flight messages
    /// (up to <see cref="BenzeneKafkaConfig.DrainTimeout"/>) and close the consumer.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();

        if (_runTask != null)
        {
            await _runTask;
        }
    }

    public void Dispose()
    {
        _stoppingCts.Dispose();
        _linkedCts?.Dispose();
        _serviceResolverFactory.Dispose();
    }
}
