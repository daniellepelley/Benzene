using System.Threading.Tasks;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.QueueStorage;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Example.Azure;

/// <summary>
/// Queue Storage trigger dispatching into the same message handlers as the HTTP function. Queue
/// messages carry no properties, so the body must be the Benzene message envelope
/// (<c>{"topic": "order.create", "headers": {}, "body": "..."}</c>) - see the Queue Storage
/// section of docs/azure-functions.md. Uses the Functions host's own storage account
/// (<c>AzureWebJobsStorage</c>) and an <c>orders</c> queue.
/// </summary>
public class QueueFunction
{
    private readonly IAzureFunctionApp _app;

    public QueueFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("orders-queue")]
    public Task Run(
        [QueueTrigger("orders", Connection = "AzureWebJobsStorage")] string messageText)
    {
        return _app.HandleQueueMessage(messageText);
    }
}
