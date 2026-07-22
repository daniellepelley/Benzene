using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.EventGrid;

// analytics-api consumes payment:captured and shipment:dispatched off Event Grid (one event → many
// subscriptions, shared with notifications), plus the HTTP Cloud Service Profile.
[assembly: BenzeneHttpTrigger(Name = "analytics")]
[assembly: BenzeneEventGridTrigger(Name = "analytics-eg")]
