# Lightweight non-HTTP Benzene messages between containers — options

## The ask
> When Benzene runs in a Docker container it generally runs as HTTP listening on a port. What would be
> amazing is if it worked like an AWS Lambda and could accept different message types, not just HTTP.
> I'd like internal Docker containers to communicate using lightweight Benzene messages — this might
> involve a different port or wrapping the message in HTTP. Investigate the options.

This is an **investigation**, not an implementation. It surveys what Benzene already has, lays out four
concrete options with trade-offs, and recommends a phased path.

> **Update (2026-07-22): Phase 1 / Option A is implemented.** The envelope client landed in the existing
> **`Benzene.Client.Http`** package (not a new `Benzene.Clients.Http` — that package was already the
> outbound-HTTP home, and its pre-existing content was a *plain-REST* caller, not the envelope client, so
> the gap below held exactly). New: `HttpBenzeneMessageClient : IBenzeneMessageClient` (POSTs
> `{ topic, headers, body }`, maps the `{ statusCode, headers, body }` response), `HttpBenzeneMessageHealthCheck`
> (non-destructive `healthcheck`-topic reachability, §3.9 permission→Warning), and
> `AddHttpBenzeneMessageClient(url, healthCheck: true)` which auto-wires the check onto the dependency
> category. Same-port per the maintainer's decision (it's ordinary middleware on the existing HTTP port).
> **Demonstrated** in `examples/K8sMesh`: the three Cloud Services now chain orders → payments → shipping
> over their neighbour's `/benzene-message` endpoint (egress `HttpBenzeneMessageClient` + ingress
> `UseBenzeneMessage`), turning that mesh from discovery-only into one with real service-to-service traffic.
> Phase 2 (generic gRPC envelope) remains unstarted.

---

## The key realisation: the "Lambda multi-event" model already exists

An AWS Lambda accepts many event shapes (API Gateway, SNS, SQS, Kafka, EventBridge) because the *host*
demuxes each source onto one set of handlers. Benzene already has the same shape, and it is **not** an
HTTP-only story:

- **The message is already transport-neutral.** Everything reduces to a `BenzeneMessageRequest`
  (`{ topic, headers, body }`, `src/Benzene.Core.Messages/BenzeneMessage/BenzeneMessageRequest.cs:3`) in
  and a `BenzeneMessageResponse` (`{ statusCode, headers, body }`) out — JSON strings — dispatched by
  `BenzeneMessageApplication.HandleAsync` (`src/Benzene.Core.MessageHandlers/BenzeneMessage/BenzeneMessageApplication.cs:20`).
  That single method is the join point every transport funnels into.
- **Workers already compose.** `CompositeBenzeneWorker` (`src/Benzene.SelfHost/CompositeBenzeneWorker.cs:5`)
  fans `StartAsync`/`StopAsync` across *every* registered `IBenzeneWorker`. A single container can already
  run an HTTP worker **and** a Kafka worker **and** a Service Bus worker at once, each feeding the same
  handler pipeline. That *is* the Lambda multi-source model — a container is not limited to one transport.
- **The worker seam is tiny.** `IBenzeneWorker` is just `StartAsync`/`StopAsync`
  (`src/Benzene.Abstractions.Pipelines/Hosting/IBenzeneWorker.cs:3`); you register one with
  `IBenzeneWorkerStartup.Add(factory)` under `UseWorker(...)`. `BoundedConcurrentDispatcher<T>`
  (`src/Benzene.SelfHost/BoundedConcurrentDispatcher.cs:38`) is a ready-made backpressured, optionally
  key-ordered dispatch primitive that Kafka/Rabbit/HTTP-self-host all reuse.

So the question is **not** "can a container accept non-HTTP messages" — architecturally it already can.
The question is: **what wire format + acceptor do we want for lightweight *container-to-container* calls,
where both ends are Benzene and we control both sides?** Four options follow.

---

## What exists today, precisely

| Surface | What it does | Gap for this goal |
|---|---|---|
| `BenzeneMessageHttpMiddleware` (`src/Benzene.Http/BenzeneMessage/BenzeneMessageHttpMiddleware.cs:37`) | **Server already accepts a raw Benzene envelope over HTTP** — `POST /benzene-message`, JSON body is deserialized straight to `BenzeneMessageRequest`, dispatched, envelope written back. Topic travels **inside the JSON body**, not a header. | **No matching outbound client.** Every `IBenzeneMessageClient` (`src/Benzene.Clients/IBenzeneMessageClient.cs:6`) is a broker/cloud SDK (SQS, SNS, Kafka, Rabbit, Service Bus, Lambda…). **There is no `HttpBenzeneMessageClient`** and no `HttpClient` anywhere under `src/Benzene.Clients*`. The receive half exists; the send half does not. |
| gRPC (`src/Benzene.Grpc`, `.AspNet`, `.Client`) | Binary, HTTP/2, cross-container, deadline + health + trailer-status aware. `GrpcBenzeneMessageClient : IBenzeneMessageClient`. | **Per-topic, per-protobuf-method.** There is **no generic "send any Benzene message" service** — each topic maps to a concrete `Method<TRequest,TResponse>` and needs a `.proto`/protobuf type + a registry entry (`GrpcClientRouteRegistry.Add<TReq,TResp>(topic, "/pkg.Service/Method")`). Great when you have contracts; heavyweight when you just want to relay an opaque envelope. |
| `BenzeneHttpWorker` (`src/Benzene.SelfHost.Http/BenzeneHttpWorker.cs:9`) | Self-hosted `HttpListener` worker; **routes by method + path**, not by topic header. | HTTP framing; path-routed, so it's the "REST-shaped" host, not an envelope relay. Combined with the middleware above it *can* host `/benzene-message`. |
| Raw TCP / Unix-domain-socket / named-pipe / WebSocket worker | — | **Does not exist.** The lowest-level acceptor in the repo is `HttpListener`. A raw acceptor is greenfield (but small — see Option C). |

---

## The options

### Option A — Turn on BenzeneMessage-over-HTTP, and add the missing client (wrap in HTTP)

Use the envelope endpoint that already exists. A caller `POST`s `{ topic, headers, body }` to
`/benzene-message`; the server dispatches it and returns `{ statusCode, headers, body }`. This is the
"wrap the message in HTTP" path the ask mentions, and the **server is already written**.

- **Same port or a second port.** It's ordinary middleware, so it rides the container's existing HTTP
  port with zero new listeners — or bind a second `BenzeneHttpWorker`/Kestrel endpoint if you want the
  internal envelope traffic isolated from public REST (see "A second port" below).
- **The one thing to build: `HttpBenzeneMessageClient : IBenzeneMessageClient`** — a thin `HttpClient`
  wrapper that serialises `BenzeneMessageClientRequest`, POSTs it, and deserialises
  `BenzeneMessageClientResponse`. Both envelope DTOs already exist
  (`src/Benzene.Clients.Aws.Lambda/BenzeneMessageClientRequest.cs`, `src/Benzene.Clients/BenzeneMessageClientResponse.cs`);
  this is ~one class plus DI wiring, and it slots into the existing `IBenzeneMessageSender`/
  `AddOutboundRouting` facade the generated clients already target.

**Pros:** smallest possible delta; reuses HTTP/2, TLS, keep-alive, load balancers, service meshes, and the
existing health-check/observability stack for free; both halves speak the identical envelope the whole
framework is built around; trivially debuggable with `curl`. **Cons:** JSON-over-HTTP has real per-call
overhead (headers, text encoding) — "lightweight" only relative to REST, not to a binary framed socket;
topic-in-body means no cheap header-based routing/filtering at a proxy.

### Option B — A generic gRPC "BenzeneMessage" envelope service (binary, reuse gRPC infra)

Add **one** fixed proto — a single unary method carrying the opaque envelope, e.g.
`rpc Send(BenzeneEnvelope) returns (BenzeneEnvelopeResponse)` where `BenzeneEnvelope = { string topic;
map<string,string> headers; bytes body }`. Unlike today's per-topic gRPC routing, this is **one method for
all topics** — the server unpacks it into a `BenzeneMessageRequest` and calls the same
`BenzeneMessageApplication`. The client is a second `IBenzeneMessageClient` implementation alongside the
existing typed `GrpcBenzeneMessageClient`.

**Pros:** binary framing, HTTP/2 multiplexing + flow-control/backpressure, deadlines, and health
(`grpc.health.v1`) are **already solved** and already in the repo; genuinely lightweight on the wire; no
per-topic `.proto` churn — new topics need no codegen. **Cons:** new (small) proto + package; `body` as
`bytes`/JSON loses gRPC's strong-typing (that's the point, but it's a philosophical split from the typed
gRPC transport — two gRPC surfaces to explain); HTTP/2 only.

### Option C — A dedicated framed-TCP self-hosted worker on a second port (true non-HTTP)

Greenfield `BenzeneTcpWorker : IBenzeneWorker` using `TcpListener`, length-prefixed frames
(`[4-byte length][envelope bytes]`) carrying the JSON (or MessagePack) envelope, dispatched through
`BoundedConcurrentDispatcher<T>` into `BenzeneMessageApplication`. This is the literal "different port,
not HTTP" reading of the ask.

**Pros:** the leanest possible wire — no HTTP verbs/paths/status lines; full control of framing and
(optionally) MessagePack for compact binary bodies; conceptually a clean sibling of the Kafka/Rabbit
workers. **Cons:** **the most to build and own** — framing, partial reads, connection lifecycle,
per-connection concurrency, graceful drain, TLS if you want it, and a bespoke health check — all things
HTTP/gRPC give you for free. You'd also need a matching `TcpBenzeneMessageClient` with connection pooling.
Reinvents a lot of transport plumbing for a marginal gain over Option B, which already has binary framing.

### Option D — Unix domain socket / named pipe (same-host sidecar only)

Same as Option C but over a UDS/named pipe instead of TCP. **Only fits same-host / sidecar topologies**
(shared network namespace or mounted socket) — it does **not** solve arbitrary container-to-container
across a Docker/Kubernetes network, which is the stated need. Worth keeping in the back pocket for a
future sidecar/agent story; **out of scope** for cross-container messaging.

---

## Cross-cutting concerns (apply to whichever option)

- **A second port.** Isolating internal envelope traffic on its own port is easy — either a second
  `BenzeneHttpWorker` binding (Option A) or the gRPC/TCP listener (B/C) is just another registered
  `IBenzeneWorker`; `CompositeBenzeneWorker` runs them side by side. This lets you expose only the public
  port through the ingress and keep `/benzene-message` (or the binary port) cluster-internal.
- **Discovery.** Container-to-container still needs "where is service X" — DNS/service names in
  Docker/K8s, or the existing **Benzene Mesh** discovery. This design doesn't change discovery; it only
  changes the wire once you have an address.
- **Health checks.** This dovetails with the health-check work just shipped: a new outbound client
  (HTTP or gRPC envelope) should **auto-wire a non-destructive reachability check on the dependency
  category** exactly like the SQS/gRPC clients do (`AddDependencyHealthCheck`, non-critical by default).
  Option A/B inherit HTTP/gRPC health probes on the *serving* side; Option C needs a bespoke one.
- **Backpressure.** Options A (HttpListener), B (gRPC), and C all funnel through
  `BoundedConcurrentDispatcher<T>`, whose bounded channel already gives backpressure and optional
  per-key ordering — no new mechanism required.
- **Serialization.** Bodies are JSON strings today. Option B/C could carry the body as raw `bytes`
  (there is already `RawBytesMessage`/`IRawBytesMessage` for a binary path) and optionally MessagePack —
  but that's an orthogonal optimisation; start with the JSON envelope for parity and debuggability.
- **Security.** `/benzene-message` intentionally exposes every routed topic (its own doc flags it as
  dev/admin, opt-in) — for internal use, keep it on the cluster-internal port and put auth (mTLS via the
  mesh, or a shared header) in front. Same posture for the binary options.

---

## Recommendation

**Phase 1 — Option A, and it's small.** The receive half already exists; the only real work is a
`HttpBenzeneMessageClient : IBenzeneMessageClient` (an `HttpClient` around the two envelope DTOs) plus
`AddHttpBenzeneMessageClient(url, healthCheck: true)` DI wiring that auto-wires a dependency health check.
That gives fully-working, debuggable container-to-container Benzene messaging over HTTP — same or second
port — for roughly one class of new code, and closes the asymmetry that today only the *server* can speak
the envelope. Ship this first; it satisfies the core need immediately.

**Phase 2 — Option B when the wire cost matters.** If/when JSON-over-HTTP overhead is measured to be a
real cost on hot internal paths, add the generic gRPC envelope service. It reuses all the gRPC framing,
flow-control, deadline, and health infrastructure Benzene already ships, so it's a bounded addition, and
it gives the genuinely "lightweight" binary transport — without the per-topic `.proto` burden of the
existing typed gRPC path.

**Defer Option C/D.** A bespoke framed-TCP worker (C) reinvents transport plumbing that Option B gets for
free from gRPC, for little marginal benefit; pursue it only if a concrete requirement rules out HTTP/2
(e.g. a non-gRPC-capable peer). UDS/named-pipe (D) is a same-host sidecar story, not cross-container, so
it's out of scope here.

**Net:** the Lambda-style "accept many message types" capability is *already* how Benzene composes
transports — a container can run several workers at once into one handler set. To make
*container-to-container* calls lightweight, the highest-leverage, lowest-risk move is to finish the
HTTP-envelope story that's already 80% built (Phase 1), then add a generic gRPC envelope service for the
binary fast path (Phase 2).

## Open questions for the maintainer
1. **Same port vs. dedicated internal port** for `/benzene-message` — do you want internal envelope
   traffic physically separated from public REST, or is one port fine with topic-level auth?
2. **Is the JSON envelope's per-call overhead actually a concern** for your internal call volumes, or is
   Phase 1 (HTTP) sufficient indefinitely and Phase 2 purely speculative?
3. **Auth model** for the internal port — rely on the mesh/mTLS, a shared secret header, or nothing
   (trusted network only)?
4. **New package placement** — `HttpBenzeneMessageClient` most naturally lives in a new
   `Benzene.Clients.Http` (mirroring `Benzene.Clients.Aws.*`); confirm you're happy adding that package
   (the "no new package without asking" rule in AGENTS.md).
