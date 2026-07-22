using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.EventGrid;
using Benzene.Azure.Function.EventHub;

// notifications-api consumes order:placed off Event Hub (its own consumer group, the fan-out partner of
// inventory), plus payment:captured and shipment:dispatched off Event Grid, plus the HTTP Cloud Service
// Profile. All declared, not hand-written.
[assembly: BenzeneHttpTrigger(Name = "notifications")]
[assembly: BenzeneEventHubTrigger(Name = "notifications-eh", EventHubName = "order-placed", Connection = "EventHubConnection", ConsumerGroup = "notifications")]
[assembly: BenzeneEventGridTrigger(Name = "notifications-eg")]
