Introducing Benzene: an open-source .NET framework for building services that outlive their first cloud provider.

Most teams don't choose AWS Lambda, Azure Functions, or a plain ASP.NET Core host once. They choose it, then a reorg happens, a customer needs on-prem, an acquisition brings a new cloud contract, or a serverless bill gets too big and someone asks "can we just run this in a container instead?"

The usual answer is: rewrite it. Business logic gets tangled up with SDK types, event shapes, and platform-specific plumbing, so "move it" really means "rebuild it."

Benzene is our answer to that. It's a hexagonal (ports-and-adapters) framework for C# built around one middleware pipeline. You write a message handler once, against a topic — not against an SQS event, an HTTP request, or a Service Bus message. Benzene's adapters translate whichever transport you're running behind (AWS Lambda, Azure Functions, ASP.NET Core, Kafka, RabbitMQ, gRPC, and more) into that same handler call.

Cross-cutting concerns — correlation IDs, logging, validation, retries, distributed tracing, health checks — live in composable middleware, written once, and apply on every transport automatically.

The part we think matters most: Benzene doesn't hide the cloud from you to get there. Your handler still gets full, direct access to the native SDK message when it needs it. We abstract your business logic, never the transport — so switching hosts doesn't mean losing the cloud-native features that were the reason you picked that provider in the first place.

It's open source (MIT), built in the open, and honest about where it stands: currently shipping prerelease packages on the way to a 1.0 that we're deliberately not rushing.

If you build or manage teams shipping event-driven or API services on .NET and have felt the pain of a platform migration turning into a rewrite, we'd love your eyes on it.

Repo: [link]
Docs / 5-minute quickstart: [link]

#dotnet #csharp #opensource #softwarearchitecture #cloudnative #hexagonalarchitecture
