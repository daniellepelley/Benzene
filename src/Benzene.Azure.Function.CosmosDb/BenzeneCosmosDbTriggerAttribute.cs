using System;

namespace Benzene.Azure.Function.CosmosDb;

/// <summary>
/// Declares a Cosmos DB change-feed-triggered Azure Function that forwards into the built
/// <c>IAzureFunctionApp</c>, so Benzene's source generator emits the <c>[Function]</c>/
/// <c>[CosmosDBTrigger]</c> class for you. The change feed is generic over your document type, so
/// <see cref="DocumentType"/> is required. Place at assembly scope; multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.CosmosDB</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneCosmosDbTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-cosmos";

    /// <summary>The document type the change feed is deserialized into (required, e.g. <c>typeof(OrderDocument)</c>).</summary>
    public Type? DocumentType { get; set; }

    /// <summary>The Cosmos database name.</summary>
    public string DatabaseName { get; set; } = "";

    /// <summary>The container name to watch.</summary>
    public string ContainerName { get; set; } = "";

    /// <summary>The app-setting name holding the Cosmos connection. Defaults to <c>CosmosDbConnection</c>.</summary>
    public string Connection { get; set; } = "CosmosDbConnection";

    /// <summary>The lease container name. Defaults to <c>leases</c>.</summary>
    public string LeaseContainerName { get; set; } = "leases";

    /// <summary>Whether to create the lease container if it doesn't exist. Defaults to <c>false</c>.</summary>
    public bool CreateLeaseContainerIfNotExists { get; set; }
}
