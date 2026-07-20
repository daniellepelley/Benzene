using System.Text;
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
    private readonly KafkaDeadLetterOptions<TKey, TValue>? _deadLetterOptions;
    private readonly CancellationTokenSource _stoppingCts = new();
    private IConsumer<TKey, TValue>? _consumer;
    private Task? _runTask;
    private CancellationTokenSource? _linkedCts;

    public BenzeneKafkaWorker(IServiceResolverFactory serviceResolverFactory,
        KafkaApplication<TKey, TValue> kafkaApplication, BenzeneKafkaConfig benzeneKafkaConfig,
        ILogger<BenzeneKafkaWorker<TKey, TValue>> logger,
        IKafkaConsumerFactory<TKey, TValue>? consumerFactory = null,
        KafkaDeadLetterOptions<TKey, TValue>? deadLetterOptions = null)
    {
        _benzeneKafkaConfig = benzeneKafkaConfig;
        _kafkaApplication = kafkaApplication;
        _serviceResolverFactory = serviceResolverFactory;
        _logger = logger;
        _consumerFactory = consumerFactory ?? new KafkaConsumerFactory<TKey, TValue>();
        _deadLetterOptions = deadLetterOptions;

        if (_deadLetterOptions is { DeadLetterTopic: not null } && _deadLetterOptions.Producer == null)
        {
            throw new InvalidOperationException(
                $"{nameof(KafkaDeadLetterOptions<TKey, TValue>)}.{nameof(KafkaDeadLetterOptions<TKey, TValue>.DeadLetterTopic)} " +
                $"is set but {nameof(KafkaDeadLetterOptions<TKey, TValue>.Producer)} is null - dead-lettering needs a caller-built producer.");
        }
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
                Func<ConsumeResult<TKey, TValue>, int>? keySelector = _benzeneKafkaConfig.PreserveOrderPerPartition
                    ? consumeResult => consumeResult.Partition.Value
                    : null;

                var handle = BuildHandle(runToken);

                dispatcher = new BoundedConcurrentDispatcher<ConsumeResult<TKey, TValue>>(
                    _benzeneKafkaConfig.ConcurrentRequests,
                    handle,
                    _logger,
                    keySelector,
                    _benzeneKafkaConfig.CatchHandlerExceptions,
                    onFault: _ => _stoppingCts.Cancel());

                // The dispatcher must exist before the consumer so the partitions-revoked handler can
                // quiesce the revoked partitions' lanes (see ConfigureRebalanceDrain). The handler runs
                // on this consume thread during Consume(), after _consumer is assigned below. When
                // draining is off there's no builder config to apply, so the original single-arg
                // Create is used - preserving behavior (and custom factories) exactly.
                var configureBuilder = ConfigureRebalanceDrain(dispatcher);
                _consumer = configureBuilder == null
                    ? _consumerFactory.Create(_benzeneKafkaConfig.ConsumerConfig)
                    : _consumerFactory.Create(_benzeneKafkaConfig.ConsumerConfig, configureBuilder);
                _consumer.Subscribe(_benzeneKafkaConfig.Topics);

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
    /// Builds the per-record handler the dispatcher runs, layering (when configured) retry-then-dead-
    /// letter over the base handle, and the manual <c>StoreOffset</c> that <c>CommitOnlyOnSuccess</c>
    /// needs. Dead-lettering catches handler faults itself (retry, then move the record aside) so a
    /// poison record neither wedges the partition nor trips the worker-stopping onFault path; only a
    /// failure to <em>produce</em> the dead-letter propagates.
    /// </summary>
    private Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> BuildHandle(CancellationToken runToken)
    {
        var commitOnSuccess = _benzeneKafkaConfig.CommitOnlyOnSuccess;
        var deadLetter = _deadLetterOptions is { IsEnabled: true } ? _deadLetterOptions : null;

        if (deadLetter == null)
        {
            return commitOnSuccess
                ? async (consumeResult, _) =>
                {
                    await _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory, runToken);
                    _consumer!.StoreOffset(consumeResult);
                }
                : (consumeResult, _) => _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory, runToken);
        }

        var maxAttempts = Math.Max(1, deadLetter.MaxAttempts);
        return async (consumeResult, _) =>
        {
            Exception? lastError = null;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await _kafkaApplication.HandleAsync(consumeResult, _serviceResolverFactory, runToken);
                    if (commitOnSuccess)
                    {
                        _consumer!.StoreOffset(consumeResult);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogWarning(ex, "Kafka handler failed (attempt {Attempt}/{MaxAttempts}) for {TopicPartitionOffset}",
                        attempt, maxAttempts, consumeResult.TopicPartitionOffset);
                }
            }

            await ProduceToDeadLetterAsync(deadLetter, consumeResult, lastError!, runToken);
            if (commitOnSuccess)
            {
                // Advance past the poison record now that it's safely re-produced to the dead-letter topic.
                _consumer!.StoreOffset(consumeResult);
            }
        };
    }

    /// <summary>
    /// Re-produces the original record to the dead-letter topic with diagnostic <c>x-dlt-*</c> headers
    /// (the failing exception's <em>type name</em> only - never its message, which could carry payload
    /// data), preserving the record's key, value, and original headers.
    /// </summary>
    private async Task ProduceToDeadLetterAsync(KafkaDeadLetterOptions<TKey, TValue> deadLetter,
        ConsumeResult<TKey, TValue> consumeResult, Exception error, CancellationToken cancellationToken)
    {
        var headers = new Headers();
        if (consumeResult.Message.Headers != null)
        {
            foreach (var header in consumeResult.Message.Headers)
            {
                headers.Add(header.Key, header.GetValueBytes());
            }
        }

        headers.Add(KafkaDeadLetterOptions<TKey, TValue>.ReasonHeader, Encoding.UTF8.GetBytes(error.GetType().Name));
        headers.Add(KafkaDeadLetterOptions<TKey, TValue>.OriginalTopicHeader, Encoding.UTF8.GetBytes(consumeResult.Topic));
        headers.Add(KafkaDeadLetterOptions<TKey, TValue>.OriginalPartitionHeader,
            Encoding.UTF8.GetBytes(consumeResult.Partition.Value.ToString()));
        headers.Add(KafkaDeadLetterOptions<TKey, TValue>.OriginalOffsetHeader,
            Encoding.UTF8.GetBytes(consumeResult.Offset.Value.ToString()));

        var message = new Message<TKey, TValue>
        {
            Key = consumeResult.Message.Key,
            Value = consumeResult.Message.Value,
            Headers = headers,
        };

        _logger.LogError(error, "Dead-lettering {TopicPartitionOffset} to {DeadLetterTopic} after {MaxAttempts} attempt(s)",
            consumeResult.TopicPartitionOffset, deadLetter.DeadLetterTopic, Math.Max(1, deadLetter.MaxAttempts));

        await deadLetter.Producer!.ProduceAsync(deadLetter.DeadLetterTopic, message, cancellationToken);
    }

    /// <summary>
    /// Builds the <see cref="ConsumerBuilder{TKey,TValue}"/> configuration that registers the
    /// partitions-revoked handler when <c>DrainOnRevoke</c> is on: on revoke it quiesces the revoked
    /// partitions' dispatcher lanes (bounded by <see cref="BenzeneKafkaConfig.DrainTimeout"/>) and
    /// commits their stored offsets before releasing them. Returns <c>null</c> when draining is off, so
    /// the consumer is built exactly as before.
    /// </summary>
    private Action<ConsumerBuilder<TKey, TValue>>? ConfigureRebalanceDrain(
        BoundedConcurrentDispatcher<ConsumeResult<TKey, TValue>> dispatcher)
    {
        if (!_benzeneKafkaConfig.ShouldDrainOnRevoke)
        {
            return null;
        }

        return builder => builder.SetPartitionsRevokedHandler((consumer, revoked) =>
        {
            try
            {
                var partitions = revoked.Select(tpo => tpo.Partition.Value).ToArray();
                // The revoked handler runs on this consume thread, so no new records are being enqueued
                // while we wait - the in-flight lanes only drain down. Blocking here is expected during
                // a rebalance.
                dispatcher.DrainLanesAsync(partitions, _benzeneKafkaConfig.DrainTimeout).GetAwaiter().GetResult();

                // Commit the offsets stored (by CommitOnlyOnSuccess's StoreOffset) for the drained
                // records, so the partition's next owner resumes after them rather than reprocessing.
                consumer.Commit();
            }
            catch (KafkaException ex)
            {
                // Nothing stored yet (e.g. no successful record on a revoked partition) surfaces as a
                // "no offset stored" commit error - benign; the partition simply advances no further.
                _logger.LogDebug(ex, "Commit during partition revoke found no stored offsets to commit.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error draining/committing revoked partitions during rebalance.");
            }
        });
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
