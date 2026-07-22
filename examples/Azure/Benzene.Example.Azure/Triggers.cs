using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.QueueStorage;
using Benzene.Azure.Function.ServiceBus;

// Triggers declared instead of hand-written: Benzene's source generator emits the [Function]/[…Trigger]
// classes that forward into the built IAzureFunctionApp. You own each trigger's name and bindings
// (route, queue, connection); Benzene writes the ceremony. Compare with the deleted HttpFunction /
// QueueFunction / ServiceBusFunction classes this replaces.
[assembly: BenzeneHttpTrigger(Name = "orders")]
[assembly: BenzeneQueueTrigger(Name = "orders-queue", QueueName = "orders", Connection = "AzureWebJobsStorage")]
[assembly: BenzeneServiceBusTrigger(Name = "orders-service-bus", QueueName = "orders", Connection = "ServiceBusConnection")]
