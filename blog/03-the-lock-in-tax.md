The most expensive line item in a cloud migration is rarely the infrastructure. It's the rewrite.

When a service's business logic is written directly against SQS event shapes, Lambda handler signatures, or Azure Function bindings, "move this to a different platform" quietly becomes "rebuild this service." That shows up as a multi-quarter project, not a config change — and it tends to surface at the worst possible moment: mid-acquisition due diligence, a renegotiated cloud contract, a serverless bill that's outgrown its budget, or a customer whose procurement rules mandate a specific cloud.

We built Benzene, an open-source .NET framework, because we kept seeing engineering teams pay that tax without ever having agreed to it up front.

Benzene separates a service's business logic from the transport it happens to run behind — AWS Lambda, Azure Functions, or a standard ASP.NET Core process. A message handler is written once; moving it to a different host is a hosting change, not a rewrite. For a leader, that turns "which cloud do we commit to" from a one-way door into a decision you can revisit later at a fraction of the cost.

Three things this tends to matter for in practice:

Portability as leverage. Multi-cloud isn't only about running two clouds at once — it's about not being structurally unable to move if your negotiating position, compliance requirements, or cost profile changes.

Consistent engineering across teams. Every service follows the same handler-plus-middleware pattern regardless of which cloud team owns it, which shortens onboarding and code review for engineers moving between teams.

Cross-cutting concerns done once. Correlation IDs, logging, validation, retries, and distributed tracing are written once as shared middleware rather than re-implemented — and re-audited — per team, per platform.

None of this requires giving anything up. Benzene deliberately does not hide AWS or Azure behind a generic interface — teams keep full access to whichever cloud-native features they chose that platform for. It's open source (MIT), and honest about its current stage: it's pre-1.0, actively developed, not a finished product pretending otherwise.

If platform lock-in has ever shown up as a risk on one of your architecture reviews, this is worth a look: [link]

#engineeringleadership #cloudstrategy #dotnet #opensource #softwarearchitecture
