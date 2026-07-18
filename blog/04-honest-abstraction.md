The most common way "multi-cloud frameworks" fail isn't that they don't work. It's that they work, and then quietly cost you the reason you picked your cloud provider in the first place.

The pattern looks like this: a framework gives you a generic "queue" interface, or a generic "state store" interface, so your code doesn't need to know if it's talking to SQS or Service Bus, DynamoDB or Cosmos DB. That sounds great in a slide deck. In practice, a generic queue interface can only expose the subset of features every queue has in common — which means the specific reasons you chose SQS (FIFO groups, batch item failures, visibility timeout tuning) or Kafka (partitions, consumer groups, exactly-once semantics) get sanded off. You paid for a specific cloud-native capability and got a lowest-common-denominator abstraction instead.

Benzene takes the opposite position on purpose.

We abstract at the business-logic boundary — your message handler doesn't know or care which transport is calling it — and never at the transport boundary. The handler's context still exposes the real, native SDK message underneath. If you need an SQS-specific feature, it's right there, not hidden behind a facade that "supports 80% of queues." A database is never wrapped either — persistence is your handler's own code (EF Core, Dapper, the AWS SDK, whatever you already use), because a database was never a transport to begin with.

We wrote this down explicitly as a Capability Matrix: for every major concern (idempotency, resilience, sagas, tracing, schema evolution), the docs state exactly what Benzene provides, what it deliberately doesn't do, and why — including the cases where the honest answer is "this needs external coordination and no framework can promise otherwise." No hidden gaps, no capabilities quietly missing until you find them in production.

For engineers, this means no surprise ceiling on what the framework will let you do. For the people signing off on the architecture, it means the tool's limits are documented up front instead of discovered during an incident.

Capability Matrix: [link]
Repo: [link]

#softwarearchitecture #dotnet #opensource #cloudnative #engineeringleadership
