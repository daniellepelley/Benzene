# Cloudflare Queues Binding — work plan

**Date:** 2026-08-13
**Status:** plan; no implementation started.
**Source research:** [cloudflare-transport-research.md](cloudflare-transport-research.md) — read it
first. This document turns its §4 sketch into pickup-cold work packages.

## How to use this document

Each work package (WP) is written to be picked up by an agent with no other context: goal, home,
prerequisites, ordered tasks, acceptance criteria, do-not list. **WP-CF0 gates everything else** —
the research it builds on was assembled from search-result snippets because this session's network
egress policy blocked `developers.cloudflare.com` directly, so every Cloudflare API claim below is
*plausible and consistently reported* but **not primary-source verified**. Do not skip it.

Home for all code: **benzene-dotnet**. Cross-port notes are in WP-CF5.

### Sequencing

```
WP-CF0 (verify the REST contract)  ──► WP-CF1 (outbound client) ──► WP-CF2 (consumer)
                                                                        │
                                        WP-CF3 (R2 event notifications) ┘  (needs CF2's consumer)
                                        WP-CF4 (example + getting-started docs) ── after CF1+CF2
                                        WP-CF5 (other ports) ── after CF2 proves the shape
```

Producer before consumer is deliberate: the client is the smaller piece, it proves the auth/config
plumbing both halves share, and you need a way to *put* messages on a queue to test the consumer.

### The support commitment — SETTLED (2026-08-13)

**Decision: Cloudflare is a committed platform. Ship Queues.** Recorded here so it isn't
re-litigated per-WP.

The reasoning, because the framing matters for how the packages get documented:

- **Cloudflare-as-a-host was already committed, and it costs nothing.** The website's platform
  list advertises `("Cloudflare", "Containers")` — a *host*, listed alongside Kubernetes and
  self-hosted VMs, not a transport list like AWS's or Azure's. That claim is accurate and free:
  the HTTP path contains **no Cloudflare-specific Benzene code**, it is `Benzene.AspNet.Core` in a
  container. Cloudflare is supported the way Kubernetes is — by virtue of ASP.NET Core being
  supported.
- **The old "experimental / out of scope for 1.0 / no API-stability guarantee" banner was
  mis-scoped in both directions** and has been fixed (benzene-dotnet `21c94c4`): it disclaimed the
  API stability of a surface whose entire API is `Benzene.AspNet.Core`'s, while under-stating the
  actual risk — that the Cloudflare-side deployment recipe has never been run against a live
  account. The three banners now carry that honest split instead.
- **Queues is the first genuine Cloudflare commitment**: real Cloudflare-specific code, real
  versioning, real support surface. It is worth making anyway, for one reason that outweighs the
  small size of the platform: Benzene is a framework for **message-driven services**, and on
  Cloudflare today you can only do HTTP. That is not a gap at the edge of the platform's coverage —
  it is the absence of the thing the framework is for. It is also the cheapest binding in the whole
  catalog to build (zero-dependency, two small packages, both near-verbatim copies of existing
  siblings).

**What the decision does NOT waive — the one real gate.** Every other transport binding in the
repo is at least fake-tested against *captured real payloads*. Without a single run against a live
Cloudflare account, these packages would ship against payload shapes believed correct from
documentation alone. **WP-CF2 must not be marked complete on a doc-only verification**: either run
it against a real queue, or ship it with the same explicit "never verified against a live account"
statement the Cloudflare example README carries, in both packages' `CLAUDE.md` *and* their NuGet
package descriptions. That is a support-burden decision, not a code-quality one, and it is the
thing to weigh — not whether to commit.

**Consequent action when WP-CF2 ships:** update the website's platform list
(`website/generator/MarketingContent.cs`) from `("Cloudflare", "Containers")` to
`("Cloudflare", "Containers, Queues")` — **not before**, so the front page stays accurate in both
directions. Also drop the "Cloudflare's own message transports have no Benzene binding yet" scope
note from `docs/getting-started-cloudflare.md`'s banner at the same time.

---

## WP-CF0 — Verify the Cloudflare Queues REST contract *(blocking spike)*

**Goal:** replace every unverified claim the research rests on with a primary-source (or better,
live-tested) fact, and write down anything that turns out differently — the plan below is shaped by
these assumptions and some of them will move.
**Home:** a scratch spike; the deliverable is an update to
[cloudflare-transport-research.md](cloudflare-transport-research.md) §2/§4 plus notes here, not code.
**Prerequisite:** a Cloudflare account with Queues enabled and an API token, **or** an explicit
decision to proceed doc-only (see the honesty rule below).

Tasks:
1. Fetch the primary docs directly (this session could not): `developers.cloudflare.com/queues/`,
   `/queues/configuration/pull-consumers/`, `/queues/configuration/batching-retries/`, and the REST
   API reference under `developers.cloudflare.com/api/resources/queues/`.
2. Pin down, exactly:
   - **Endpoints + auth**: confirm `POST /accounts/{account_id}/queues/{queue_id}/messages`,
     `/messages/batch`, `/messages/pull`, `/messages/ack`; the auth header shape (API token vs
     legacy key); which account/queue identifiers are needed and whether a queue is addressed by
     id, name, or both.
   - **The pull/lease model**: what `POST /messages/pull` accepts (batch size, visibility timeout
     equivalent), what a returned message looks like *on the wire* (the research says `body`, `id`,
     `timestamp_ms`, `attempts`, `metadata`, `lease_id` — confirm), how long a lease lasts, and
     whether there is any long-poll/wait parameter (**this materially changes the poll loop**: with
     a wait parameter it mirrors SQS long-polling; without one, WP-CF2 needs its own poll interval
     and idle backoff, which is a different config surface).
   - **The ack semantics**: confirm `/messages/ack` takes *both* acks and retries in one call
     (`acks[]` + `retries[]` by `lease_id`), and what happens to a lease that is simply never acked
     (does it redeliver on lease expiry — the SQS visibility-timeout equivalent — and does that
     count against `max_retries`?). WP-CF2's ack-mode design depends entirely on this.
   - **Message body constraints**: the 128 KB per-message / 256 KB per-batch limits, and — the one
     that matters most for topic resolution — **confirm there is genuinely no arbitrary
     user-settable per-message header/attribute channel** (the research says `metadata` holds
     Cloudflare-internal fields like `CF-Content-Type` only). If a user-settable attribute channel
     *does* exist, WP-CF2's topic resolution should prefer it over envelope-in-body, matching the
     SQS/Service Bus convention instead of the Queue Storage one — a real fork in the design.
   - **Fan-out**: confirm the research's "no SNS equivalent" claim — specifically whether one queue
     can have multiple independent consumers/consumer groups each receiving every message. This is
     stated as a product limitation in the research and would be repeated in user-facing docs, so
     it needs to be right.
3. If an account is available, exercise the four endpoints by hand (curl) against a scratch queue
   and save the real request/response payloads — these become WP-CF2's test fixtures.
4. Update the research note with what was confirmed, corrected, or still unknown.

Acceptance: every "confirm" above is answered with a primary-source link or a captured live
response, and the research note's §6 egress caveat is replaced with what was actually verified.

**Honesty rule (house convention — see benzene-go's `CLAUDE.md` "Do NOT fabricate deployment
config" and the standing Tempo caveat in `.claude/PRODUCT_OWNERS.md`):** if no Cloudflare account
is available, say so explicitly in the research note and in every package `CLAUDE.md` — "verified
against documentation, never against a live Cloudflare account" — rather than implying the binding
is battle-tested. Do not fabricate a response shape you have not seen.

Do NOT: start WP-CF1/CF2 code before this lands; assume the SQS long-poll shape transfers without
checking for a wait parameter.

---

## WP-CF1 — `Benzene.Clients.Cloudflare.Queues` (outbound)

**Goal:** publish a Benzene message to a Cloudflare Queue, satisfying the standard outbound-client
interface so every existing decorator (correlation id, trace context, retry) composes unchanged.
**Home:** benzene-dotnet, new `src/Benzene.Clients.Cloudflare.Queues/`.
**Prerequisite:** WP-CF0.
**Template to copy:** `src/Benzene.Clients.Azure.QueueStorage/` — it is the closest existing sibling
in every respect that matters (envelope-as-body, no native header channel, `OutboundContext`
converter + client middleware + health check). Read its `CLAUDE.md` first, especially its
"**Routing — there is no property to set; the envelope IS the routing**" section.

Tasks:
1. Scaffold the package mirroring `Benzene.Clients.Azure.QueueStorage`'s file set:
   `QueuesBenzeneMessageClient`, `QueuesClientMiddleware`, `OutboundQueuesContextConverter`,
   `QueuesSendMessageContext`, `QueuesHealthCheck`, `Extensions` (a `UseCloudflareQueues(...)`
   on the outbound-routing pipeline builder, matching `.UseSqs(...)`/`.UseQueueStorage(...)`).
2. Send: `POST /accounts/{account}/queues/{queue}/messages` with the serialized
   `BenzeneMessageRequest` envelope (`Topic`, `Headers`, `Body`) as the message body — the Queue
   Storage convention verbatim, **unless WP-CF0 found a real per-message attribute channel**, in
   which case use it for the topic and headers and put only the payload in the body.
3. Status mapping: a successful publish → `accepted` (fire-and-forget; no response channel), a
   transport/HTTP failure → `service-unavailable`, matching every other queue-shaped outbound
   client (`awssqs`, `azurequeuestorage`, `azureservicebus` in the Go port; `Benzene.Clients.*` in
   .NET).
4. **Zero third-party dependencies.** Cloudflare ships no .NET SDK for Queues, so this is
   `System.Net.Http` + `System.Text.Json` only. That is a genuine selling point over
   `Benzene.Aws.Sqs` (which needs `AWSSDK.SQS`) — state it in the package `CLAUDE.md`. Take the
   `HttpClient` via `IHttpClientFactory`/injection rather than constructing one, so tests use a
   fake handler and no live account is needed.
5. Credentials: the API token comes from configuration/environment, **never** a constructor
   literal or anything committed. Follow the repo's existing stance (the mesh enterprise note's
   "credentials never live in config; config names endpoints, secrets come from the
   environment/secret stores").
6. Tests against a fake `HttpMessageHandler` asserting the request URL, auth header presence, and
   serialized envelope shape; a `CLAUDE.md` for the package; a row in `docs/capability-matrix.md`.
7. Register the project in `Benzene.sln` and add it to `test/Benzene.Core.Test`'s
   `ProjectReference` list (that project references essentially every package).

Acceptance: unit tests green against a fake handler; a live publish verified by hand if an account
exists (and explicitly marked unverified if not); `dotnet build` of the solution clean.
Do NOT: add any third-party dependency; wrap Cloudflare transport features Benzene doesn't abstract
(delays, per-message `contentType` beyond what the envelope needs) — the Benzene.Aws.Sqs `CLAUDE.md`
sets this boundary explicitly ("Benzene abstracts message publishing at the business-logic layer and
does not wrap the SDK's own transport features"); put the API token anywhere near source control.

---

## WP-CF2 — `Benzene.Cloudflare.Queues` (inbound consumer) — the main event

**Goal:** a long-running polling consumer that pulls from a Cloudflare Queue over the REST API,
dispatches each message through the Benzene pipeline (one invocation + one DI scope per message),
and acks/retries per outcome — making a Docker-hosted Benzene service a first-class Queues consumer
with **no Worker in the message path**.
**Home:** benzene-dotnet, new `src/Benzene.Cloudflare.Queues/`.
**Prerequisite:** WP-CF0; WP-CF1 (for a way to enqueue test messages).
**Templates to copy:** `src/Benzene.Aws.Sqs/Consumer/` for the worker/poll-loop shape (14 files:
`SqsConsumer`, `SqsConsumerApplication`, `SqsConsumerConfig`, `SqsConsumerOptions`,
`SqsConsumerAckMode`, `SqsConsumerMessageContext`, the getters/setters/mappers, and
`Extensions.UseSqs`); `src/Benzene.Azure.Function.QueueStorage/` for **topic resolution**.

Tasks:
1. **Topic resolution — support both paths, exactly as Queue Storage does.** This is the design
   heart of the WP and the research note under-specified it (it named only the envelope path):
   - **Envelope path**: the message body deserializes to a `BenzeneMessageRequest` (`Topic`,
     `Headers`, `Body`) → route on its `Topic`. Copy `BenzeneMessageQueueStorageHandler`, which is
     a `MiddlewareRouter<BenzeneMessageRequest, TContext>` whose `TryExtractRequest` deserializes
     the message text and whose `CanHandle` requires a non-null `Topic`.
   - **Preset-topic path**: for a queue fed by a **non-Benzene producer** (an R2 event
     notification — see WP-CF3 — or any third-party writer), the body is the payload itself and the
     topic is fixed per queue. Copy the `PresetTopicHolder` / `PresetTopicMessageTopicGetter`
     wrapping in `Benzene.Azure.Function.QueueStorage`'s `DependencyInjectionExtensions`, exposed as
     `queues.UsePresetTopic("orders:created").UseMessageHandlers()`.
   Both must work; a Cloudflare queue is as likely to carry foreign events as Benzene ones.
2. Poll loop (`IBenzeneWorker`, `StartAsync(CancellationToken)`), copying `SqsConsumer`'s proven
   behaviors — these are hard-won and must not be re-derived from scratch:
   - loop until the token is signaled;
   - a poll iteration that throws for a non-cancellation reason is **logged and the loop continues
     after a capped, geometrically-growing backoff** (reset on the next successful receive) — never
     propagate out and kill the worker, never hot-spin;
   - if WP-CF0 found no long-poll/wait parameter, add an explicit idle `PollInterval` (the
     `azurecosmos.Worker` precedent in benzene-go, which paces empty polls for exactly this reason)
     and document why it exists where SQS needs none.
3. **Ack semantics.** Mirror `SqsConsumerAckMode` (`PerMessage` default = safe: ack only messages
   whose handler reported explicit success; a failure/throw/unrouted message is left for
   redelivery — plus `WholeBatch` for the all-or-nothing alternative). Cloudflare's `/messages/ack`
   taking acks **and** retries in one call is a better fit than SQS's delete-only API: a failed
   message can be *explicitly* retried rather than merely left to lease expiry. Confirm against
   WP-CF0 whether explicit retry resets or advances the `attempts` count, and document it — this
   determines whether a poison message reaches the configured DLQ as expected.
4. Scope/failure rules per [transport-bindings.md](../docs/specification/transport-bindings.md) §1:
   exactly one pipeline invocation and one DI scope **per message, not per batch**; one bad message
   never poisons the batch or crashes the host.
5. `Extensions.UseCloudflareQueues(...)` on `IBenzeneWorkerStartup`, matching `UseSqs`'s signature
   shape (config, client factory, inner-pipeline action, optional options configure).
6. Zero third-party dependencies (same as WP-CF1); `HttpClient` injected so tests use a fake handler
   driven by WP-CF0's captured payloads.
7. Tests: fake-handler tests for the poll→dispatch→ack cycle in both ack modes, both topic-resolution
   paths, a failing handler leaving the message unacked, and a throwing poll iteration backing off
   rather than escaping. Package `CLAUDE.md`. A `docs/capability-matrix.md` row stating the
   at-least-once/failure semantics in the same voice as the existing SQS and Queue Storage rows.
   `Benzene.sln` + test-project references.

Acceptance: tests green; an end-to-end round trip (WP-CF1 publishes → this consumer handles) run by
hand against a real queue if an account exists, explicitly marked unverified if not; a deliberately
failing handler demonstrably leaves the message for redelivery.
Do NOT: put a Worker in the message path (the entire point is that the pull API removes the need);
dispatch a whole batch as one invocation (§1.6 says per message); re-derive the poll-loop error
handling — copy `SqsConsumer`'s; silently drop a message whose body is neither a valid envelope nor
matched by a preset topic (report it, per the no-silent-drop rule benzene-go's `awss3` binding
documents).

---

## WP-CF3 — R2 Event Notifications

**Goal:** document (and prove with a test/example) that R2 object events reach Benzene handlers,
with a topic convention consistent with the existing S3 binding.
**Home:** benzene-dotnet — **documentation and an example, not a new package.**
**Prerequisite:** WP-CF2.

The key insight, and why this is cheap: **R2 Event Notifications deliver into a Cloudflare Queue.**
There is no separate transport to bind — WP-CF2's consumer already receives them. This WP is only
(a) the topic convention and (b) proving it.

Tasks:
1. Confirm the R2 notification message shape as delivered onto the queue (WP-CF0's primary-source
   pass may already have it; if not, fetch `developers.cloudflare.com/r2/buckets/event-notifications/`).
2. Choose the topic convention. **Recommended: `{bucket}:{eventType}`** — matching
   `Benzene.Aws.Lambda.S3`'s `{bucketName}:{eventName}` and `Benzene.Aws.Lambda.DynamoDb`'s
   `{tableName}:{eventName}`. Note the deliberate divergence already recorded in benzene-go's
   `CLAUDE.md` (its `awss3` routes on the bare event name because "the S3 topic is a local routing
   concern, not a wire contract") — so this is a per-port choice to make consciously, not a wire
   contract. Since an R2 notification is a foreign (non-Benzene) producer, this rides WP-CF2's
   **preset-topic path**, or a small mapper that derives the topic from the notification's own
   fields.
3. Document it in the Cloudflare getting-started guide (WP-CF4) with the bucket-side setup: an R2
   event notification rule pointing at the queue WP-CF2 consumes.
4. A test using a captured R2 notification payload as a fixture.

Acceptance: a documented, tested path from "object created in R2" to "Benzene handler runs", with
the topic convention stated and justified.
Do NOT: build a separate `Benzene.Cloudflare.R2` transport package — it would duplicate WP-CF2's
consumer for no gain; invent a topic convention without noting the S3/DynamoDB precedent.

---

## WP-CF4 — Example + getting-started docs

**Goal:** the Cloudflare story stops being HTTP-only in the docs, and there's a runnable example.
**Home:** benzene-dotnet: `examples/Cloudflare/` (extend the existing project) and
`docs/getting-started-cloudflare.md`.
**Prerequisite:** WP-CF1 + WP-CF2.

Tasks:
1. Extend `examples/Cloudflare` with a queue producer + consumer alongside the existing HTTP
   handler, in **one container** — demonstrating the composition that makes this worth having:
   `UseHttp(...)` for the Worker-proxied HTTP surface and the queues consumer as a background
   worker in the same process, exactly as a service today can run `UseHttp` and `Benzene.Aws.Sqs`'s
   consumer side by side.
2. Add a "Queues" section to `docs/getting-started-cloudflare.md`: `wrangler` queue creation, the
   API token's required permissions (least-privilege — name the exact scope), the config the
   consumer needs, and the envelope-vs-preset-topic choice.
3. Update the example's README with what was and wasn't verified against a live account (the
   existing README already carries this caveat for the Worker/container config — extend it rather
   than dropping it).
4. Land the two consequent edits the settled decision defers to this point (see "The support
   commitment" above): drop the "Cloudflare's own message transports … have no Benzene binding yet"
   scope note from the guide's banner, and update the website's platform list
   (`website/generator/MarketingContent.cs`, in the **Benzene** repo, not this one) from
   `("Cloudflare", "Containers")` to `("Cloudflare", "Containers, Queues")`. Both are deliberately
   held until the binding actually ships.

Acceptance: the example builds and runs locally against a real queue (or is explicitly marked
doc-verified-only); the website generator's broken-link self-check still passes if any cross-repo
links were added.
Do NOT: present untested deploy config as verified (`benzene-go`/`benzene-dotnet` both have an
explicit rule about this); let the example drift from the guide's code.

---

## WP-CF5 — Other language ports *(later; do not start without a demonstrated need)*

The reason this is worth recording rather than dropping: **Cloudflare Queues' REST API makes this
the cheapest cross-port transport Benzene has.** Every other queue binding needs a vendor SDK per
language (`AWSSDK.SQS`, `aws-sdk-go-v2`, `azservicebus`, `segmentio/kafka-go` …). Cloudflare Queues
needs an HTTP client and a JSON serializer — which every port already has in its standard library.
A Go/TypeScript/Python Cloudflare Queues consumer would be **zero-dependency**, which for benzene-go
in particular (whose `CLAUDE.md` treats "zero dependencies is itself a selling point" as a first-class
constraint, and quarantines every SDK-needing binding into its own module) means it could live in the
**root module** rather than needing its own — unlike `awssqs`, `kafka`, `azureservicebus`, and the
rest.

Trigger to start: WP-CF2 shipped and the shape proven in .NET, **plus** a real user need for
Cloudflare on another port. Do not speculatively port.

---

## Cross-cutting rules

- **Verify before building on an assumption.** WP-CF0 exists because this plan's Cloudflare facts
  are search-snippet-derived, not primary-source. The same rule has already reshaped three WPs in
  [third-party-tool-integrations-plan.md](third-party-tool-integrations-plan.md) — assumptions in
  these plans have a poor track record.
- **Copy the sibling, don't re-derive it.** `Benzene.Aws.Sqs/Consumer/` and
  `Benzene.Azure.Function.QueueStorage/` between them already solve the poll loop, the ack modes,
  the backoff, and both topic-resolution paths. Divergence from them should be a documented
  decision, not an accident.
- **Zero third-party dependencies** in both new packages — no Cloudflare .NET SDK exists, and none
  is needed. Keep it that way.
- **Credentials from configuration/environment only**, never source.
- **No silent drops**: an unroutable message is reported, not swallowed.
- **Nothing here touches the spec.** A new transport binding is an adapter; `transport-bindings.md`
  §3.1 ("the core never references a vendor") holds. The one *optional* spec-adjacent follow-up is
  adding Cloudflare Queues to `transport-bindings.md` §2's informative catalog of worked examples
  once it ships — additive, informative, no conformance impact.
- Keep commits scoped to one logical change; new capability = package + tests + docs in the same
  commit (benzene-dotnet's stated workflow expectation).
