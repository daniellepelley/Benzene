Quick question for anyone maintaining more than three Lambda functions or Azure Functions: how many places is your correlation-ID logic copy-pasted into right now? Your retry logic? Your request validation?

This is the unglamorous cost of transport-first code. Every Function, every Lambda handler, every Controller action ends up with its own thin layer of "stuff that has nothing to do with the business logic" wrapped around the part that does — logging, correlation IDs, input validation, retry-with-backoff, trace-context propagation. It's duplicated because each one is a separate entry point with no shared pipeline underneath it. Fix a bug in your retry logic and you're grepping the whole repo for every place it got pasted.

Benzene's middleware pipeline exists specifically to kill this. Every adapter — HTTP, SQS, SNS, Kafka, Service Bus, Event Hubs, RabbitMQ, whatever — runs through the same `IMiddleware<TContext>` pipeline before it reaches your handler. Write the middleware once:

.UseCorrelationId()
.UseW3CTraceContext()
.UseRetry(numberOfRetries: 3, ...)
.UseValidation()

...and it applies on every transport your service runs behind, not just the one you remembered to add it to. Adding a new transport to an existing service doesn't mean re-implementing the cross-cutting concerns for it; it means pointing the same pipeline at a new adapter.

It also means these concerns are unit-testable in isolation, independent of any specific cloud SDK — no need to spin up a fake Lambda context to verify your retry backoff math.

We built this because "logging middleware exists in transport A but someone forgot to port it to transport B" is a genuinely common way production incidents happen, not a hypothetical.

If your services are accumulating this kind of duplication, take a look: [docs link]

#dotnet #csharp #softwareengineering #middleware #cloudnative
