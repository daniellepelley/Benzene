using Benzene.Azure.Function.AspNet;

// The catch-all HTTP trigger, declared instead of hand-written: Benzene's source generator emits the
// [Function]/[HttpTrigger] class that forwards into the built IAzureFunctionApp (serving /benzene/spec,
// /benzene/health, /benzene/invoke, and the domain's routes). Default methods/route (catch-all) match
// what the mesh needs.
[assembly: BenzeneHttpTrigger(Name = "service")]
