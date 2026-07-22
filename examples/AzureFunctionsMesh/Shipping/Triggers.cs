using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.ServiceBus;

// shipping-api consumes the shipment:book command off its Service Bus queue, and serves the Cloud Service
// Profile over HTTP. Both triggers are declared, not hand-written.
[assembly: BenzeneHttpTrigger(Name = "shipping")]
[assembly: BenzeneServiceBusTrigger(Name = "shipping-sb", QueueName = "shipping", Connection = "ServiceBusConnection")]
