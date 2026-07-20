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
    private readonly IKafkaConsumerFactory<TKey, TValue> _consumerFactory;
    private readonly CancellationTokenSource _stoppingCts = new();
    private IConsumer<TKey, TValue>? _consumer;
    private Task? _runTask;
    private CancellationTokenSource? _linkedCts;

    public BenzeneKafkaWorker(IServiceResolverFactory serviceResolverFactory,
        KafkaApplication<TKey, TValue> kafkaApplication, BenzeneKafkaConfig benzeneKafkaConfig,
        ILogger<BenzeneKafkaWorker<TKey, TValue>> logger,
        IKafkaConsumerFactory<TKey, TValue>? consumerFactory = null)
    {
        _benzeneKafkaConfig = benzeneKafkaConfig;
        _kafkaApplication = kafkaApplication;
        _serviceResolverFactory = serviceResolverFactory;
        _logger = logger;
        _consumerFactory = consumerFactory ?? new KafkaConsumerFactory<TKey, TValue>();
    }

    /// <summary>
    /// Starts the consume loop on a background task and returns immediately - it does not wait for
    /// the loop to run to completion. Use <see cref="StopAsync"/> to signal shutdown and wait for
    /// in-flight messages to drain.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_benzeneKafkaConfig.CommitOnlyOnSuccess)
        {
            if (_benzeneKafkaConfig.CatchHandlerExceptions)
            {
                throw new InvalidOperationException(
                    $"{nameof(BenzeneKafkaConfig.CommitOnlyOnSuccess)} requires " +
                    $"{nameof(BenzeneKafkaConfig.CatchHandlerExceptions)} = false - otherwise a handler " +
                    "exception is swallowed and the message's offset would never be stored, but later, " +
                    "successful messages on the same partition would still advance the commit watermark " +
                    "past it.");
            }

            if (!_benzeneKafkaConfig.PreserveOrderPerPartition)
            {
                throw new InvalidOperationException(
                    $"{nameof(BenzeneKafkaConfig.CommitOnlyOnSuccess)} requires " +
                    $"{nameof(BenzeneKafkaConfig.PreserveOrderPerPartition)} = true - otherwise a " +
                    "partition's messages can be handled out of order, and storing a later message's " +
                    "offset first would advance the commit watermark past an earlier one still in flight.");
            }

            _benzeneKafkaConfig.ConsumerConfig.EnableAutoOffsetStore = false;
        }

        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingCts.Token);
        var runToken = _linkedCts.Token;

        _runTask = Task.Run(async () =>
        {
            BoundedConcurrentDispatcher<ConsumeResult<TKey, TValue>>? dispatcher = null;

            try
            {
                _consumer = _consumerFactory.Create(_benzeneKafkaConfig.ConsumerConfig);
                _consumer.Subscribe(_benzeneKafkaConfig.Topics);

                Func<ConsumeResult<TKey, TValue>, int>? keySelector = _benzeneKafkaConfig.PreserveOrderPerPartition
                    ? consumeResult => consumeResult.Partition.Value
                    : null;

                Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> handle = _benzeneKafkaConfig.CommitOnlyOnSuccess
                    ? async (consumeResult, _) =>
                    {
                        await _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory, runToken);
                        _consumer!.StoreOffset(consumeResult);
                    }
                    : (consumeResult, _) => _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory, runToken);

                dispatcher = new BoundedConcurrentDispatcher<ConsumeResult<TKey, TValue>>(
                    _benzeneKafkaConfig.ConcurrentRequests,
                    handle,
                    _logger,
                    keySelector,
                    _benzeneKafkaConfig.CatchHandlerExceptions,
                    onFault: _ => _stoppingCts.Cancel());

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

                        // A single bad message aside, a persistently failing broker/connection would
                        // otherwise spin this loop as fast as it can fail - back off before retrying.
                        // Cancellable via runToken so shutdown stays responsive during the delay.
                        await Task.Delay(_benzeneKafkaConfig.ConsumeExceptionRetryDelay, runToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown - fall through to drain and close below.
            }
            catch (Exception ex)
            {
                // Anything unexpected here - including consumer/subscribe setup failures, and any
                // KafkaException other than ConsumeException - is logged so the loop's death is
                // visible, rather than leaving the worker silently dead with a faulted, unobserved
                // _runTask. Cleanup below still runs on this path.
                _logger.LogCritical(ex, "Unhandled exception in Kafka consume loop; worker is stopping.");
            }
            finally
            {
                if (dispatcher != null)
                {
                    await dispatcher.DrainAsync(_benzeneKafkaConfig.DrainTimeout);
                }

                _consumer?.Close();
                _consumer?.Dispose();
            }
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
