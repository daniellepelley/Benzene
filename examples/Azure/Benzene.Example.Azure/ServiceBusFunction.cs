using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Example.Azure;

/// <summary>
/// Service Bus trigger dispatching into the same message handlers as the HTTP function: the
/// message routes by its <c>"topic"</c> application property (e.g. <c>order.create</c>) and its
/// body is the payload - see docs/cookbooks/service-bus-handling.md. Requires a
/// <c>ServiceBusConnection</c> app setting (connection string, or
/// <c>ServiceBusConnection__fullyQualifiedNamespace</c> for managed identity - see
/// docs/cookbooks/managed-identity.md) and an <c>orders</c> queue.
/// </summary>
public class ServiceBusFunction
{
    private readonly IAzureFunctionApp _app;

    public ServiceBusFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("orders-service-bus")]
    public Task Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        return _app.HandleServiceBusMessages(message);
    }
}
