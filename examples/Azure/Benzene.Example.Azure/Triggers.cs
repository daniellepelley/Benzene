using Benzene.Azure.Function.AspNet;

// The HTTP trigger, declared instead of hand-written: Benzene's source generator emits the
// [Function]/[HttpTrigger] class that forwards into the built IAzureFunctionApp. We own the name
// ("orders") and the catch-all route; Benzene writes the ceremony. Compare with the still-hand-written
// QueueFunction/ServiceBusFunction in this project (those transports get the same treatment next).
[assembly: BenzeneHttpTrigger(Name = "orders")]
