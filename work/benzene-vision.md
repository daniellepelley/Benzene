# Benzene Vision

**Status:** Living reference document
**Last Updated:** 2026-07-12
**Purpose:** Capture the original problem, philosophy, and design principles behind
Benzene so future design decisions can be checked against the original intent, not just
against what happens to be convenient at the time.

**Source:** This document is derived from Daniel Le Pelley's experience report,
["Microservices in Serverless Functions: An Experience Report"](https://www.digiterre.com/2023/06/06/microservices-in-serverless-functions-an-experience-report/)
(Digiterre, 2023-06-06), reconciled against the current state of the codebase.

---

## 1. The Problem Benzene Exists to Solve

Benzene grew out of a real production engagement: a large B2B software system, built
entirely on AWS Lambda, serving thousands of businesses across the UK. The team went in
expecting the standard serverless pitch — lower cost, effortless scaling, less
infrastructure to maintain — and ran into the standard serverless pathology at scale:

- **Function proliferation.** A "one Lambda per endpoint/event" discipline, applied
  consistently across a large system, produces hundreds of small functions. The
  granularity that looks clean in a diagram becomes a fragmentation problem in practice.
- **Duplicated logic across functions.** Related functions end up re-implementing the
  same cross-cutting concerns (logging, correlation, validation, error handling)
  slightly differently each time, because there's no shared place for that code to live.
- **Hard to test and debug.** Business logic entangled with the specific shape of a
  Lambda event (API Gateway payload, SQS record, SNS envelope) is hard to unit test
  without mocking AWS infrastructure, and hard to reason about once deployed.
- **Vendor lock-in.** Code written directly against `APIGatewayProxyRequest` or
  `SQSEvent` doesn't move to another cloud, or even to a non-Lambda host, without a
  rewrite.
- **Brittleness from interdependency.** Many small, independently deployed functions
  wired together by convention (naming, IAM, event configuration) creates a system
  that is easy to break and hard to see the shape of.

The founding insight was that these problems are not inherent to serverless — they are
inherent to *fine-grained, transport-coupled* function design. The fix is not to
abandon serverless, but to change the unit of deployment and decouple business logic
from the transport that invokes it.

> "If you can get all the advantages of using a serverless runtime but without the
> downsides, it would be a step-change in how large software projects can be delivered."

---

## 2. Core Design Philosophy

### 2.1 A service is defined by what it does, not by its transport

This is the central organizing idea behind Benzene:

> "A service should be defined by what it does, not by its transport."

Business logic (a message handler) should not know or care whether it was invoked by an
HTTP request, an SQS message, an SNS notification, a Kafka record, or a direct in-process
call. The handler's signature is `IMessageHandler<TRequest, TResponse>` — nothing about
API Gateway, SQS, or ASP.NET Core leaks into it. Transport is a deployment detail,
swappable without touching the handler.

**Design-decision test:** if implementing a feature requires a message handler to import
a transport-specific type (`APIGatewayProxyRequest`, `HttpContext`, `SQSEvent`, etc.),
something has gone wrong — that concern belongs in an adapter, not the handler.

### 2.2 Microservices-in-a-Lambda, not microservices-as-many-Lambdas

Benzene's answer to function proliferation is to consolidate: an entire microservice —
potentially handling many message topics across many transports — deploys as a single
Lambda (or a single ASP.NET Core process, or a single Azure Function app). Granularity
is chosen at the *service* boundary, not forced down to the *individual endpoint/event*
boundary by the constraints of the runtime.

This is a deliberate trade-off, not an oversight:

- **Non-goal:** fine-grained, single-responsibility-per-function deployment. Benzene
  does not optimize for "one Lambda per event source."
- **Goal:** fewer, larger-grained deployable units, each internally organized around
  message handlers and topics, each independently testable without any AWS
  infrastructure.

**Design-decision test:** does a proposed feature push developers back toward creating
more, smaller deployable units to work around a limitation? If so, it's working against
the model, not with it.

### 2.3 Hexagonal architecture (ports and adapters) as the structuring discipline

Message handlers are the application core. They depend only on `Benzene.Abstractions`
(`IMessageHandler<TRequest, TResponse>`, `IBenzeneResult<T>`) and whatever port
interfaces the handler author defines for its own dependencies (a database, another
service). Everything else — API Gateway, SQS, SNS, Kafka, EventBridge, Azure Functions,
ASP.NET Core, a database driver, a cache — is an adapter living outside the core.

Each transport adapter's job is narrow and mechanical: convert its native request into
Benzene's universal message shape, route it to the matching handler, convert the
`IBenzeneResult` back into a transport-native response. The adapter should contain as
little logic as possible; the interesting logic lives in the core, where it is
transport-agnostic and directly testable.

**Design-decision test:** would this change be easier to write as core logic + a thin
adapter, or does it only make sense bolted onto one specific transport? If the latter,
question whether it belongs in Benzene at all, or should live in the consuming
application instead.

### 2.4 One middleware pipeline, shared by every adapter

Inspired by Express.js and ASP.NET Core middleware, Benzene provides a single
composable middleware pipeline (`Benzene.Core.Middleware`) that every transport adapter
runs through. Cross-cutting concerns — correlation IDs, logging, validation, health
checks, exception handling — are written once as middleware and apply uniformly across
every transport, rather than being reimplemented (and drifting) per adapter.

**Design-decision test:** is this cross-cutting concern being added to the shared
pipeline, or is it being duplicated into each adapter separately? Duplication here is
exactly the failure mode Benzene exists to prevent.

### 2.5 A universal message format

All transport-specific payloads are normalized into a common shape — a topic and a
body — before they reach handler-selection logic. This is what lets a single
`[Message("hello:world")]`-attributed handler be reachable from HTTP, Lambda events, or
a queue without transport-specific dispatch code in the handler layer.

### 2.6 Convention over manual wiring

New message handlers are discovered automatically by reflection, keyed by topic. There
is no manual routing table to maintain as handlers are added or removed. This keeps the
"many handlers, one deployable unit" model tractable — the whole point of consolidation
is undermined if adding a handler also means updating a hand-maintained router.

### 2.7 Multi-cloud portability as a consequence, not a bolt-on

Because handlers depend only on `Benzene.Abstractions` and never on a specific cloud
SDK's event types, the same handler code runs unmodified behind AWS Lambda, Azure
Functions, or a plain ASP.NET Core host. Portability isn't a separate feature to build —
it falls out naturally from taking transport-agnosticism seriously. If a design choice
would only work on one cloud, that is a signal the abstraction boundary has been drawn
in the wrong place.

---

## 3. What Benzene Optimizes For (and What It Deliberately Doesn't)

**Optimizes for:**
- Business logic that is defined once and is genuinely transport-agnostic
- Testability without mocking cloud infrastructure — a handler can be tested as a plain
  function call
- Consolidation: fewer deployable units, each handling many related concerns
- Reuse of cross-cutting concerns (logging, validation, correlation, health checks)
  across every transport via one middleware pipeline
- Freedom to change or add transports later without rewriting business logic
- Letting the team "focus much more on the code and less on the runtime" — infrastructure
  concerns should recede, not dominate

**Deliberately does not optimize for:**
- Fine-grained, single-responsibility-per-function deployment (the opposite of the
  proliferation problem Benzene was built to solve)
- Being the thinnest possible wrapper around any one cloud provider's native event model
  — some abstraction overhead is an accepted cost of transport-agnosticism
- Being a general-purpose serverless framework for teams who *want* many small,
  independently-scaled functions — that's a legitimate architecture, just not the one
  Benzene is designed around

---

## 4. Using This Document

When evaluating a new feature, package, or breaking change for Benzene, check it against
section 2's five principles:

1. Does it keep handlers ignorant of transport?
2. Does it support consolidation rather than push toward more, smaller deployables?
3. Does it respect the ports-and-adapters boundary (thin adapters, logic in the core)?
4. Does it belong in the shared middleware pipeline rather than being duplicated per
   adapter?
5. Does it preserve or improve multi-cloud portability, rather than special-casing one
   provider?

A change that fails more than one of these is worth pausing on — not necessarily
rejecting, but worth an explicit conversation about why this case should be the
exception.

---

## 5. Related Documents

- [`README.md`](../README.md) — current-state overview of the framework and quickstart
- [`docs/message-handlers.md`](../docs/message-handlers.md) — the handler pattern in
  practice
- [`docs/middleware.md`](../docs/middleware.md) — the shared pipeline in practice
- [`work/aws-roadmap-1.0.md`](aws-roadmap-1.0.md), [`work/azure-roadmap-1.0.md`](azure-roadmap-1.0.md) —
  transport-specific roadmaps that should stay consistent with this vision
