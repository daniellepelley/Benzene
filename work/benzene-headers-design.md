# `benzene-headers` — packed headers, and the chained topic getter

**Status:** PROPOSAL — accepted in shape; **execution plan is `work/benzene-headers-plan.md`**, deferred until after the repo split. Originally: proposal for maintainer ruling — captures the maintainer's design (2026-07-25), grounded
against the current code, with the edge cases that need a decision called out. Task #33.
**Last Updated:** 2026-07-25
**Purpose:** Rename `_benzeneHeaders` in line with every other header, and generalise it from an
EventBridge-only workaround into an **optional** packed-headers mode available on any transport —
without making it the default, because separate headers are what you want when you're debugging.

---

## 0. The word you were reaching for

**kebab-case** (lowercase-kebab; also called dash-case, lisp-case or spinal-case). It is what every
other Benzene header uses — `benzene-topic`, `benzene-status`, `benzene-version`, `content-type`.

## 1. What exists today

`_benzeneHeaders` is **EventBridge-only** and is not really a header at all: EventBridge has no
per-message metadata channel, so headers are embedded as a JSON object inside the payload
(`detail`) and lifted back out on the way in.

| | |
|---|---|
| Written by | `OutboundEventBridgeContextConverter` / `EventBridgeContextConverter<T>` (`EmbeddedHeadersKey`) |
| Read by | `EventBridgeMessageHeadersGetter` (same constant) |
| Topic on that binding | **not** in the bag — it rides the EventBridge `detail-type` |
| Everywhere else | headers are native (SQS/SNS attributes, Service Bus/Event Hub properties, Kafka/RabbitMQ headers, Pub/Sub attributes) and there is no packed option at all |

There are **25** `IMessageTopicGetter<TContext>` implementations, each reading one key from one
place. There is no chaining or composition for them (`CompositeMessageHandlersFinder` is the
nearest precedent in the codebase, for a different abstraction).

## 2. The proposal

### 2.1 Rename: `_benzeneHeaders` → `benzene-headers`

Same kebab-case as every other header. This **deliberately breaks the "JSON fields are camelCase"
rule** where the bag is payload-embedded, and that is the right call: it is a *header name* that
happens to be carried in a payload because the transport has nowhere else to put it — not a payload
field. Worth one explicit sentence in `wire-contracts.md` so it reads as a decision.

The leading underscore goes: `benzene-` already marks it as reserved, which is the whole point of
the naming principle. Two markers for one job is one too many.

### 2.2 Packed mode: optional everywhere, mandatory only where forced

> One transport header carrying **all** Benzene headers, as a JSON object.

- **Transports with native metadata** — packed mode is **opt-in**. Default stays one header per
  value, because when you are testing, or staring at a queue in a console, separate headers are
  simply easier to read. Packed exists for **space**: SQS allows only **10 message attributes**, and
  a service using topic + version + correlation + trace context is already at five before the
  application adds anything.
- **Transports with no metadata channel** (EventBridge) — packed is the *only* option and stays the
  default there. Not a choice, a consequence.

### 2.3 Inbound: the default topic getter becomes a chain

Per the maintainer's design — a chain-of-responsibility over the ways a topic can arrive:

1. **Native carrier**, where the binding has one (EventBridge `detail-type`, Kafka's own topic).
2. **The `benzene-topic` header/attribute.**
3. **Inside `benzene-headers`** — parse the bag, take `benzene-topic` from it.

First hit wins; nothing found ⇒ unresolved, exactly as today. The single-purpose getters stay
available and unchanged (`SqsMessageTopicGetter` reads one attribute), a new packed-only getter is
added, and **the default becomes a composite of them** — so a deployment that wants strictly one
behaviour can still register exactly that getter.

### 2.4 Inbound: the same rule for the whole header bag

The headers getter merges rather than picking: **the packed bag is the base layer, individual
headers are overlaid on top, and an individual header wins on conflict.** Same precedence as the
topic — explicit beats packed — so one sentence covers both and there is no case where the topic
and the other headers disagree about which source is authoritative.

### 2.5 Outbound: default separate, switchable to packed

- **Default:** write `benzene-topic` (and the rest) as individual headers.
- **Packed mode:** take the accumulated header dictionary, **add the topic into it**, serialise to
  JSON, write it as the single `benzene-headers` attribute.
- **Middleware stays oblivious.** Headers accumulate in the outbound dictionary as middleware adds
  them; packing happens **once, at the terminal converter**. So "headers are additive with
  middleware" holds in both modes, and no middleware needs to know which mode is configured. This
  falls out of packing last — worth stating as the invariant that keeps it true.

## 3. Decisions needed (my recommendations)

1. **Value encoding — JSON object, string-valued.** `{"benzene-topic":"order:create",
   "x-correlation-id":"…"}`. Flat string→string, matching the header dictionary exactly. No nesting.
   *Recommend: yes.* It is what EventBridge already does, and it is trivial in any language.
2. **Is the topic always in the bag?** In packed mode, yes — self-contained is one rule rather than
   a per-binding table. Bindings with a native carrier (EventBridge `detail-type`) simply prefer the
   native one on read, per §2.3's precedence. *Recommend: always include.*
3. **A `benzene-headers` key nested inside `benzene-headers`** — ignore it, flatten once, never
   recurse. *Recommend: yes, and say so normatively; it is the obvious malformed-input case.*
4. **Size.** Packed mode trades attribute *count* for attribute *size*. SQS message attributes count
   against the 256 KB message limit, so packing is a win on count and neutral-to-negative on bytes.
   *Recommend: document the trade-off, do not enforce a limit.*
5. **Config surface.** One switch per client (`packHeaders: true`) rather than per-header choices.
   *Recommend: yes — a per-header split would produce wire shapes nobody can predict.*

## 4. Scope split — what is go-live critical and what is not

| Piece | Critical for 1.0? | Why |
|---|---|---|
| **Rename** `_benzeneHeaders` → `benzene-headers` | **Yes** | Wire contract; free now, a major-version migration after the tag. Clean break, consistent with the topic-id ruling (no installed base). |
| Packed mode on other transports | No | Purely **additive** — new opt-in capability, no existing behaviour changes. |
| Chained default topic getter | No | Additive: it only *adds* a fallback after the existing lookup. Nothing that resolves today stops resolving. |
| Outbound packed switch | No | Opt-in, default unchanged. |

**Recommendation: do the rename now** (with the other go-live-critical spec work), and land the
generalisation as a follow-on that can arrive before or after the tag without breaking anyone.

## 5. Blast radius

- **Rename:** 2 constants (`EventBridgeMessageHeadersGetter.EmbeddedHeadersKey` and the outbound
  converter's, which already aliases it), their doc comments, `wire-contracts.md` §2,
  `transport-bindings.md`'s EventBridge section, and any EventBridge test fixture. Small and
  contained — the constant is already shared between the two sides.
- **Generalisation:** a packed-headers reader/writer helper (shared, one implementation), a composite
  `IMessageTopicGetter`, a composite headers getter, and per-transport registration wiring for the
  opt-in switch. Additive; no existing getter changes behaviour.
