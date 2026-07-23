using Benzene.Abstractions.DI;
using Benzene.Kafka.Core.KafkaMessage;
using Confluent.Kafka;

namespace Benzene.Kafka.Core.TestHelpers;

/// <summary>
/// A test host that drives the Kafka message pipeline a <c>StartUp</c> configured, without a running
/// broker. Built by <see cref="BenzeneTestHostExtensions.BuildKafkaWorkerHost{TStartUp, TKey, TValue}"/>;
/// push a record through it with <see cref="HandleAsync(ConsumeResult{TKey, TValue})"/> (build one from
/// a <c>MessageBuilder</c> via <c>AsKafkaBenzeneMessage()</c>).
/// </summary>
/// <typeparam name="TKey">The Kafka record key type.</typeparam>
/// <typeparam name="TValue">The Kafka record value type.</typeparam>
public sealed class KafkaBenzeneTestHost<TKey, TValue> : IDisposable
{
    private readonly KafkaApplication<TKey, TValue> _application;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    /// <summary>Initializes a new instance of the <see cref="KafkaBenzeneTestHost{TKey, TValue}"/> class.</summary>
    /// <param name="application">The built Kafka message application.</param>
    /// <param name="serviceResolverFactory">The resolver factory the application runs each record against.</param>
    public KafkaBenzeneTestHost(KafkaApplication<TKey, TValue> application, IServiceResolverFactory serviceResolverFactory)
    {
        _application = application;
        _serviceResolverFactory = serviceResolverFactory;
    }

    /// <summary>
    /// Runs a record through the pipeline exactly as <c>BenzeneKafkaWorker</c> would.
    /// </summary>
    /// <param name="record">The record to handle.</param>
    /// <returns>A task that completes when the record has been handled.</returns>
    public Task HandleAsync(ConsumeResult<TKey, TValue> record)
    {
        return _application.HandleAsync(record, _serviceResolverFactory);
    }

    /// <summary>Disposes the resolver factory (and the service provider it owns).</summary>
    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }
}
