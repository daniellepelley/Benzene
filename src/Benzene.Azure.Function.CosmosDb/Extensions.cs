using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Azure.Function.Core;

namespace Benzene.Azure.Function.CosmosDb;

/// <summary>
/// Provides extension methods for dispatching a Cosmos DB Change Feed batch to a built
/// <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Dispatches a batch of changed documents — as delivered by the Azure Functions
    /// <c>CosmosDBTrigger</c> binding — to the Azure Function app's Cosmos DB Change Feed entry point
    /// application for <typeparamref name="TDocument"/>.
    /// </summary>
    /// <typeparam name="TDocument">The document type the change feed batch was deserialized into.</typeparam>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="documents">The changed documents to handle.</param>
    /// <returns>A task that completes when the batch has been handled.</returns>
    public static Task HandleCosmosDbChanges<TDocument>(this IAzureFunctionApp source, IReadOnlyList<TDocument> documents)
    {
        return source.HandleAsync(documents);
    }
}
