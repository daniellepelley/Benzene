using Microsoft.Azure.Cosmos;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// Default <see cref="ICosmosChangeFeedProcessorFactory{TDocument}"/>: builds a manual-checkpoint
/// <see cref="ChangeFeedProcessor"/> over a monitored container and a lease container the caller
/// supplies (both created from whatever <c>CosmosClient</c>/authentication the caller chose). The
/// lease container must already exist - the processor does not create it.
/// </summary>
/// <typeparam name="TDocument">The document type the change feed batches are deserialized into.</typeparam>
public class CosmosChangeFeedProcessorFactory<TDocument> : ICosmosChangeFeedProcessorFactory<TDocument>
{
    private readonly Container _monitoredContainer;
    private readonly Container _leaseContainer;
    private readonly string _processorName;
    private readonly string _instanceName;
    private readonly Action<ChangeFeedProcessorBuilder>? _configure;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosChangeFeedProcessorFactory{TDocument}"/> class.
    /// </summary>
    /// <param name="monitoredContainer">The container whose change feed is consumed.</param>
    /// <param name="leaseContainer">The container the processor stores its leases/checkpoints in (must exist).</param>
    /// <param name="processorName">
    /// The processor name, shared by every instance cooperating on the same feed - it prefixes the
    /// lease documents, so two different processor names over the same container each get the full
    /// feed independently.
    /// </param>
    /// <param name="instanceName">
    /// This instance's unique name within the processor, used for lease ownership/load balancing
    /// across instances (e.g. the host name).
    /// </param>
    /// <param name="configure">
    /// Optional extra builder configuration (<c>WithPollInterval(...)</c>, <c>WithMaxItems(...)</c>,
    /// <c>WithStartTime(...)</c>, ...), applied after the instance name and lease container.
    /// </param>
    public CosmosChangeFeedProcessorFactory(Container monitoredContainer, Container leaseContainer,
        string processorName, string instanceName, Action<ChangeFeedProcessorBuilder>? configure = null)
    {
        _monitoredContainer = monitoredContainer;
        _leaseContainer = leaseContainer;
        _processorName = processorName;
        _instanceName = instanceName;
        _configure = configure;
    }

    /// <inheritdoc />
    public ChangeFeedProcessor Create(
        Container.ChangeFeedHandlerWithManualCheckpoint<TDocument> onChanges,
        Container.ChangeFeedMonitorErrorDelegate onError)
    {
        var builder = _monitoredContainer
            .GetChangeFeedProcessorBuilderWithManualCheckpoint(_processorName, onChanges)
            .WithInstanceName(_instanceName)
            .WithLeaseContainer(_leaseContainer)
            .WithErrorNotification(onError);

        _configure?.Invoke(builder);

        return builder.Build();
    }

    /// <inheritdoc />
    public ChangeFeedProcessor CreateAllVersionsAndDeletes(
        Container.ChangeFeedHandler<ChangeFeedItem<TDocument>> onChanges,
        Container.ChangeFeedMonitorErrorDelegate onError)
    {
        var builder = _monitoredContainer
            .GetChangeFeedProcessorBuilderWithAllVersionsAndDeletes(_processorName, onChanges)
            .WithInstanceName(_instanceName)
            .WithLeaseContainer(_leaseContainer)
            .WithErrorNotification(onError);

        _configure?.Invoke(builder);

        return builder.Build();
    }
}
