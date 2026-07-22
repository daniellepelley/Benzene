using Benzene.Azure.Function.AspNet;

// orders-api's only inbound surface is HTTP (order:create arrives via POST /orders, and the mesh
// interrogates /benzene/*). Declared, not hand-written: the source generator emits the [Function]/
// [HttpTrigger] catch-all that forwards into the Benzene pipeline.
[assembly: BenzeneHttpTrigger(Name = "orders")]
