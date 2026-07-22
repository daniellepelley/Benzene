using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace BenzeneStarter;

// One Service Bus trigger hands every message to Benzene, which routes it to a handler by the
// message's "topic" application property - so you add message handlers, not new Functions.
public class ServiceBusFunction
{
    private readonly IAzureFunctionApp _app;

    public ServiceBusFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("service-bus")]
    public Task Run(
        [ServiceBusTrigger("hello_world", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        return _app.HandleServiceBusMessages(message);
    }
}
