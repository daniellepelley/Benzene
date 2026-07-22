using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.QueueStorage;
using Microsoft.Azure.Functions.Worker;

namespace BenzeneStarter;

// One Queue Storage trigger hands each message to Benzene, which routes it to a handler by the topic
// carried in the message envelope. You add message handlers, not new Functions.
public class QueueFunction
{
    private readonly IAzureFunctionApp _app;

    public QueueFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("queue")]
    public Task Run(
        [QueueTrigger("hello-world", Connection = "StorageConnection")] string messageText)
    {
        return _app.HandleQueueMessage(messageText);
    }
}
