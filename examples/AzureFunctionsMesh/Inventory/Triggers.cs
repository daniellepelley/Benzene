using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.EventGrid;
using Benzene.Azure.Function.EventHub;

// inventory-api consumes order:placed off Event Hub (its own consumer group) and shipment:dispatched
// off Event Grid, plus the HTTP Cloud Service Profile. All declared, not hand-written — the source
// generator emits the [Function] classes that forward into the Benzene pipeline.
[assembly: BenzeneHttpTrigger(Name = "inventory")]
[assembly: BenzeneEventHubTrigger(Name = "inventory-eh", EventHubName = "order-placed", Connection = "EventHubConnection", ConsumerGroup = "inventory")]
[assembly: BenzeneEventGridTrigger(Name = "inventory-eg")]
