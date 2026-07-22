using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.Timer;

// The mesh's triggers, declared instead of hand-written: Benzene's source generator emits the
// [Function] classes forwarding into the built IAzureFunctionApp.
//  - HTTP (get/post/options): serves /mesh-ui, the catalog artifacts, and POST /mesh/refresh.
//  - Timer: the scheduled discovery + aggregation pass (the Consumption-plan replacement for the Web
//    App mesh's BackgroundService); RunOnStartup warms the catalog on a cold start.
[assembly: BenzeneHttpTrigger(Name = "mesh-http", Methods = new[] { "get", "post", "options" })]
[assembly: BenzeneTimerTrigger(Name = "aggregate", Schedule = "0 */1 * * * *", RunOnStartup = true)]
