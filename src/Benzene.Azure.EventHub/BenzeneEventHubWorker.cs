using System.Collections.Concurrent;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.EventHub;

/// <summary>
/// A long-running worker that consumes an Event Hub directly via an <see cref="EventProcessorClient"/>
/// and dispatches each event through the middleware pipeline - for
/// <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>, not Azure Functions (use
/// <c>Benzene.Azure.Function.EventHub</c> for an Event Hub trigger).
/// </summary>
/// <remarks>
/// This is one of the "self-hosted worker" startup modes documented in <c>docs/hosting.md</c> -
/// like <c>BenzeneKafkaWorker</c>, Benzene owns the process here. Unlike the SQS/Kafka workers,
/// nothing is polled by hand: the <see cref="EventProcessorClient"/> owns partition ownership,
/// load balancing across worker instances (via its blob checkpoint store), and per-partition
/// sequential dispatch (partitions run concurrently; one event at a time within a partition -
/// the same ordering promise as <c>BenzeneKafkaConfig.PreserveOrderPerPartition</c>).
/// <see cref="StartAsync"/> starts the processor and returns; <see cref="StopAsync"/> stops it,
/// waiting for in-flight handlers to finish. Checkpointing is per partition, every
/// <see cref="BenzeneEventHubConfig.CheckpointInterval"/> successfully handled events - a failed
/// event is never itself checkpointed, but see
/// <see cref="BenzeneEventHubConfig.CatchHandlerExceptions"/> for what happens to it next.
/// </remarks>
public class BenzeneEventHubWorker : IBenzeneWorker
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly EventHubConsumerApplication _application;
    private readonly BenzeneEventHubConfig _config;
    private readonly IEventProcessorClientFactory _clientFactory;
    private readonly ConcurrentDictionary<string, int> _uncheckpointedCounts = new();
    private EventProcessorClient? _processor;
    private int _stopInitiated;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneEventHubWorker"/> class.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each event.</param>
    /// <param name="application">The application that runs each event through the middleware pipeline.</param>
    /// <param name="config">The checkpointing and failure-handling behavior to use.</param>
    /// <param name="clientFactory">The factory used to create the underlying processor client.</param>
    public BenzeneEventHubWorker(IServiceResolverFactory serviceResolverFactory,
        EventHubConsumerApplication application, BenzeneEventHubConfig config,
        IEventProcessorClientFactory clientFactory)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _application = application;
        _config = config;
        _clientFactory = clientFactory;
    }

    /// <summary>
    /// Creates the processor and starts it. Returns once the processor is running - it does not
    /// block until shutdown. Use <see cref="StopAsync"/> to stop consuming and wait for in-flight
    /// handlers to finish.
    /// </summary>
    /// <param name="cancellationToken">The token used to abort startup.</param>
    /// <returns>A task that completes when the processor has started.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _processor = _clientFactory.Create();
        _processor.ProcessEventAsync += OnProcessEventAsync;
        _processor.ProcessErrorAsync += OnProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the processor, waiting for in-flight event handlers to finish and partition ownership
    /// to be relinquished.
    /// </summary>
    /// <param name="cancellationToken">The token used to abort the wait.</param>
    /// <returns>A task that completes when the processor has stopped.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
        }
    }

    private async Task OnProcessEventAsync(ProcessEventArgs args)
    {
        if (!args.HasEvent)
        {
            return;
        }

        try
        {
            await _application.HandleAsync(args.Data, _serviceResolverFactory);
        }
        catch (Exception ex)
        {
            using (var loggingScope = _serviceResolverFactory.CreateScope())
            {
                loggingScope.GetService<ILogger<BenzeneEventHubWorker>>()
                    .LogError(ex, "Processing event with sequence number {sequenceNumber} on partition {partitionId} failed",
                        args.Data.SequenceNumber, args.Partition.PartitionId);
            }

            if (!_config.CatchHandlerExceptions)
            {
                // The processor's docs forbid calling StopProcessingAsync from inside a handler
                // (it deadlocks waiting for this very handler to return), so initiate the stop on
                // a background task; StopProcessingAsync is safe to call concurrently with a
                // subsequent StopAsync. The failed event is not checkpointed, so a restart resumes
                // from the last checkpoint and redelivers it.
                if (Interlocked.Exchange(ref _stopInitiated, 1) == 0)
                {
                    _ = Task.Run(() => _processor!.StopProcessingAsync());
                }
            }

            return;
        }

        // ProcessEventAsync is invoked one event at a time per partition, so this count has no
        // same-partition race - the dictionary only needs to be concurrent across partitions.
        var seen = _uncheckpointedCounts.AddOrUpdate(args.Partition.PartitionId, 1, (_, count) => count + 1);
        if (seen >= _config.CheckpointInterval)
        {
            await args.UpdateCheckpointAsync(args.CancellationToken);
            _uncheckpointedCounts[args.Partition.PartitionId] = 0;
        }
    }

    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        using var loggingScope = _serviceResolverFactory.CreateScope();
        loggingScope.GetService<ILogger<BenzeneEventHubWorker>>()
            .LogError(args.Exception, "Event Hub processing failed during {operation} (partition {partitionId})",
                args.Operation, args.PartitionId);
        return Task.CompletedTask;
    }
}
