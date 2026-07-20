using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// A long-running worker that consumes a Service Bus queue or topic subscription directly via a
/// <see cref="ServiceBusProcessor"/> and dispatches each received message through the middleware
/// pipeline - for <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>, not Azure Functions (use
/// <c>Benzene.Azure.Function.ServiceBus</c> for a Service Bus trigger).
/// </summary>
/// <remarks>
/// This is one of the "self-hosted worker" startup modes documented in <c>docs/hosting.md</c> -
/// like <c>BenzeneKafkaWorker</c>, Benzene owns the process here. Unlike the SQS/Kafka workers,
/// nothing is polled by hand: the <see cref="ServiceBusProcessor"/> owns receiving, lock renewal,
/// and bounded concurrency (<see cref="BenzeneServiceBusConfig.MaxConcurrentCalls"/>) itself, and
/// pushes each message to this worker's handler. <see cref="StartAsync"/> starts the processor and
/// returns; <see cref="StopAsync"/> stops it, waiting for in-flight handlers to finish, then
/// disposes the processor and client. Receive-side failures (e.g. a transient connection error)
/// surface through the processor's error handler, are logged, and the processor keeps receiving -
/// they never end the worker.
/// </remarks>
public class BenzeneServiceBusWorker : IBenzeneWorker
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly ServiceBusConsumerApplication _application;
    private readonly BenzeneServiceBusConfig _config;
    private readonly IServiceBusClientFactory _clientFactory;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneServiceBusWorker"/> class.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each message.</param>
    /// <param name="application">The application that runs each message through the middleware pipeline.</param>
    /// <param name="config">The entity to consume and the processing behavior to use.</param>
    /// <param name="clientFactory">The factory used to create the underlying Service Bus client.</param>
    public BenzeneServiceBusWorker(IServiceResolverFactory serviceResolverFactory,
        ServiceBusConsumerApplication application, BenzeneServiceBusConfig config,
        IServiceBusClientFactory clientFactory)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _application = application;
        _config = config;
        _clientFactory = clientFactory;
    }

    /// <summary>
    /// Validates the configuration, creates the processor, and starts it. Returns once the
    /// processor is running - it does not block until shutdown. Use <see cref="StopAsync"/> to
    /// stop consuming and wait for in-flight messages to finish.
    /// </summary>
    /// <param name="cancellationToken">The token used to abort startup.</param>
    /// <returns>A task that completes when the processor has started.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration doesn't identify exactly one entity - either <see cref="BenzeneServiceBusConfig.QueueName"/>
    /// or both <see cref="BenzeneServiceBusConfig.TopicName"/> and
    /// <see cref="BenzeneServiceBusConfig.SubscriptionName"/> must be set.
    /// </exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Validate(_config);

        _client = _clientFactory.Create();

        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = _config.AckMode == ServiceBusConsumerAckMode.AutoComplete,
            MaxConcurrentCalls = _config.MaxConcurrentCalls,
            PrefetchCount = _config.PrefetchCount,
        };

        _processor = !string.IsNullOrEmpty(_config.QueueName)
            ? _client.CreateProcessor(_config.QueueName, options)
            : _client.CreateProcessor(_config.TopicName!, _config.SubscriptionName!, options);

        _processor.ProcessMessageAsync += OnProcessMessageAsync;
        _processor.ProcessErrorAsync += OnProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the processor - waiting for in-flight message handlers to finish - then disposes it
    /// and the client.
    /// </summary>
    /// <param name="cancellationToken">The token used to abort the wait for in-flight handlers.</param>
    /// <returns>A task that completes when the processor has stopped and been disposed.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
            _processor = null;
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
    }

    private async Task OnProcessMessageAsync(ProcessMessageEventArgs args)
    {
        if (_config.AckMode == ServiceBusConsumerAckMode.AutoComplete)
        {
            // The processor settles from whether this handler throws: complete on return, abandon
            // on throw (surfacing the exception to OnProcessErrorAsync for logging either way).
            await _application.HandleAsync(args.Message, _serviceResolverFactory, args.CancellationToken);
            return;
        }

        try
        {
            var messageResult = await _application.HandleAsync(args.Message, _serviceResolverFactory, args.CancellationToken);

            if (messageResult?.IsSuccessful == false)
            {
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            }
            else
            {
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
        }
        catch (Exception ex)
        {
            // The rethrow surfaces the exception to OnProcessErrorAsync, but that only has the
            // entity/error-source - not which message failed. Log here with the message id so a
            // failure is diagnosable to a specific message, matching the other workers (SQS/Kafka).
            using (var loggingScope = _serviceResolverFactory.CreateScope())
            {
                loggingScope.GetService<ILogger<BenzeneServiceBusWorker>>()
                    .LogError(ex, "Processing Service Bus message {messageId} failed", args.Message.MessageId);
            }

            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            throw;
        }
    }

    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        using var loggingScope = _serviceResolverFactory.CreateScope();
        loggingScope.GetService<ILogger<BenzeneServiceBusWorker>>()
            .LogError(args.Exception, "Service Bus processing for {entityPath} failed during {errorSource}",
                args.EntityPath, args.ErrorSource);
        return Task.CompletedTask;
    }

    private static void Validate(BenzeneServiceBusConfig config)
    {
        var hasQueue = !string.IsNullOrEmpty(config.QueueName);
        var hasSubscription = !string.IsNullOrEmpty(config.TopicName) && !string.IsNullOrEmpty(config.SubscriptionName);

        if (hasQueue == hasSubscription)
        {
            throw new InvalidOperationException(
                $"{nameof(BenzeneServiceBusConfig)} must identify exactly one entity: set either " +
                $"{nameof(BenzeneServiceBusConfig.QueueName)}, or both {nameof(BenzeneServiceBusConfig.TopicName)} " +
                $"and {nameof(BenzeneServiceBusConfig.SubscriptionName)}.");
        }
    }
}
