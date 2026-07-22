using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.ServiceBus;

// payments-api consumes the payment:take command off its Service Bus queue, and also serves the Cloud
// Service Profile over HTTP. Both triggers are declared, not hand-written — the source generator emits
// the [Function] classes that forward into the Benzene pipeline (Service Bus routes by the "topic"
// application property).
[assembly: BenzeneHttpTrigger(Name = "payments")]
[assembly: BenzeneServiceBusTrigger(Name = "payments-sb", QueueName = "payments", Connection = "ServiceBusConnection")]
