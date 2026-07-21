using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.RabbitMq.RabbitMqMessage;
using Benzene.SelfHost;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Benzene.RabbitMq;

/// <summary>
/// A long-running worker that consumes a RabbitMQ queue via the RabbitMQ.Client v7 async API and
/// dispatches each delivery through a Benzene middleware pipeline - for
/// <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>, not a cloud trigger. One of the "self-hosted
/// worker" startup modes documented in <c>docs/hosting.md</c>; the RabbitMQ counterpart of
/// <c>BenzeneKafkaWorker</c> and <c>BenzeneServiceBusWorker</c>.
/// </summary>
/// <remarks>
/// Deliveries are pushed by an <see cref="AsyncEventingBasicConsumer"/> (not hand-polled like Kafka)
/// and fanned out through <see cref="BoundedConcurrentDispatcher{T}"/> so up to
/// <see cref="RabbitMqConfig.ConcurrentRequests"/> handlers run at once; the consumer's prefetch
/// (QoS) count bounds how many unacknowledged deliveries the broker sends. Under
/// <see cref="RabbitMqAckMode.Explicit"/> (the default) each delivery is acked on handler success and
/// nacked - requeued or dead-lettered per <see cref="RabbitMqConfig.RequeueOnFailure"/> - on a
/// failure result or a thrown exception, so nothing is settled until its handler has actually run.
/// <see cref="StartAsync"/> opens the connection/channel and starts consuming, then returns;
/// <see cref="StopAsync"/> cancels the consumer, drains in-flight handlers (up to
/// <see cref="RabbitMqConfig.DrainTimeout"/>), and closes the channel and connection.
/// </remarks>
public class RabbitMqWorker : IBenzeneWorker, IDisposable
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly RabbitMqApplication _application;
    private readonly RabbitMqConfig _config;
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqWorker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private BoundedConcurrentDispatcher<BasicDeliverEventArgs>? _dispatcher;
    private string? _consumerTag;

    /// <summary>Initializes a new instance of the <see cref="RabbitMqWorker"/> class.</summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each delivery.</param>
    /// <param name="application">The application that runs each delivery through the middleware pipeline.</param>
    /// <param name="config">The queue to consume and the processing behavior to use.</param>
    /// <param name="connectionFactory">The factory used to open the RabbitMQ connection.</param>
    /// <param name="logger">Logs handler faults and settlement failures.</param>
    public RabbitMqWorker(IServiceResolverFactory serviceResolverFactory, RabbitMqApplication application,
        RabbitMqConfig config, IRabbitMqConnectionFactory connectionFactory, ILogger<RabbitMqWorker> logger)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _application = application;
        _config = config;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Opens the connection and channel, sets the prefetch QoS, and starts the consumer. Returns once
    /// consuming has begun - it does not block until shutdown.
    /// </summary>
    /// <param name="cancellationToken">A token to abort startup.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var autoAck = _config.AckMode == RabbitMqAckMode.AutoAck;

        // The dispatcher starts its lane tasks as soon as it is constructed, before the (fallible)
        // connection is opened. If any step below throws, StartAsync propagates but those lanes keep
        // running - so tear everything back down on failure (StopAsync is null-guarded on the
        // channel/connection/tag it may not have set yet) rather than leaking ConcurrentRequests idle
        // lane tasks on a failed startup.
        _dispatcher = new BoundedConcurrentDispatcher<BasicDeliverEventArgs>(
            _config.ConcurrentRequests,
            (delivery, _) => HandleDeliveryAsync(delivery, autoAck),
            _logger);

        try
        {
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.BasicQosAsync(0, _config.PrefetchCount, false, cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnReceivedAsync;

            _consumerTag = await _channel.BasicConsumeAsync(_config.QueueName, autoAck, consumer, cancellationToken);
        }
        catch
        {
            await StopAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Cancels the consumer, drains in-flight handlers (up to <see cref="RabbitMqConfig.DrainTimeout"/>),
    /// then closes and disposes the channel and connection.
    /// </summary>
    /// <param name="cancellationToken">A token to abort the shutdown steps.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null && _consumerTag != null)
        {
            try
            {
                // Stop new deliveries first, so the drain below only has to wait for handlers already
                // in flight (and their acks) rather than a moving target.
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cancelling RabbitMQ consumer {ConsumerTag} failed during shutdown.", _consumerTag);
            }

            _consumerTag = null;
        }

        if (_dispatcher != null)
        {
            await _dispatcher.DrainAsync(_config.DrainTimeout);
            _dispatcher = null;
        }

        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs deliverEventArgs)
    {
        // BasicDeliverEventArgs reuses a rented body buffer that is only valid for the duration of
        // this callback, so copy it before handing the delivery to another thread via the dispatcher.
        var copied = new BasicDeliverEventArgs(
            deliverEventArgs.ConsumerTag,
            deliverEventArgs.DeliveryTag,
            deliverEventArgs.Redelivered,
            deliverEventArgs.Exchange,
            deliverEventArgs.RoutingKey,
            deliverEventArgs.BasicProperties,
            deliverEventArgs.Body.ToArray(),
            deliverEventArgs.CancellationToken);

        var dispatcher = _dispatcher;
        if (dispatcher != null)
        {
            await dispatcher.EnqueueAsync(copied, CancellationToken.None);
        }
    }

    private async Task HandleDeliveryAsync(BasicDeliverEventArgs delivery, bool autoAck)
    {
        if (autoAck)
        {
            // The broker already acknowledged on dispatch; just run the handler (best effort).
            await _application.HandleAsync(delivery, _serviceResolverFactory);
            return;
        }

        try
        {
            var messageResult = await _application.HandleAsync(delivery, _serviceResolverFactory);

            if (messageResult?.IsSuccessful == false)
            {
                await NackAsync(delivery);
            }
            else
            {
                await AckAsync(delivery);
            }
        }
        catch (Exception ex)
        {
            // A thrown handler exception settles exactly like a failure result - nack (requeue/DLX) -
            // rather than leaving the delivery unacknowledged until the channel closes.
            _logger.LogError(ex, "Handling RabbitMQ delivery {DeliveryTag} threw; nacking.", delivery.DeliveryTag);
            await NackAsync(delivery);
        }
    }

    private async Task AckAsync(BasicDeliverEventArgs delivery)
    {
        var channel = _channel;
        if (channel == null)
        {
            return;
        }

        try
        {
            await channel.BasicAckAsync(delivery.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Acking RabbitMQ delivery {DeliveryTag} failed.", delivery.DeliveryTag);
        }
    }

    private async Task NackAsync(BasicDeliverEventArgs delivery)
    {
        var channel = _channel;
        if (channel == null)
        {
            return;
        }

        // Requeue is bounded to one retry: a first-attempt failure is requeued; an already-redelivered
        // failure is nacked without requeue (to the DLX / dropped) so a poison message can't hot-loop.
        var requeue = _config.RequeueOnFailure && !delivery.Redelivered;

        try
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nacking RabbitMQ delivery {DeliveryTag} failed.", delivery.DeliveryTag);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _serviceResolverFactory.Dispose();
    }
}
