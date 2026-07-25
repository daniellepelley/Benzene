# The Benzene Naming Principle — RULING

**Status:** ✅ **ACCEPTED by the maintainer, 2026-07-25** — adopted exactly as proposed, including
both carve-outs and all three consequences. **Binding on the work that applies it** (tasks #29,
#30); the spec text is updated as part of that application, not ahead of it, so the spec never
describes names the code doesn't use.
**Last Updated:** 2026-07-25
**Purpose:** Settle, once, how Benzene names the things it owns on the wire, so that the three
open naming questions (`topic` header, `benzene-version`, reserved-topic prefixes —
`work/spec-review-2026-07-25.md` §3c/§3f/§4) stop being three debates. Decision only; the
migration work is tasks #29/#30.

---

## The finding that reframes this

**Benzene already has this convention. It is applied nearly everywhere. The topic surface is where
it was never applied.** This is not "adopt a new naming scheme" — it is "finish applying our own".

Evidence from the codebase and spec:

| Surface | Benzene-invented name | Prefixed today? |
|---|---|---|
| HTTP paths | `/benzene/health`, `/benzene/invoke`, `/benzene/spec`, `/benzene/spec-ui`, `/benzene/mesh-ui` | ✅ (`design-principles.md` §5.2 defines the `/benzene/` prefix) |
| Headers | `benzene-status`, `benzene-version` | ✅ |
| Azure Functions trigger bindings | `benzene-kafka`, `benzene-service-bus`, `benzene-event-hub`, `benzene-event-grid`, `benzene-queue`, `benzene-blob`, `benzene-cosmos`, `benzene-timer` | ✅ |
| Health-check registrations | `benzene-liveness`, `benzene-readiness` | ✅ |
| Test-payload transport dressing | `benzene-message`, `benzene-test` | ✅ |
| Embedded payload key | `_benzeneHeaders` | ✅ (marked, different form — see §4) |
| **Header for the topic** | **`topic`** | ❌ |
| **Reserved topic ids** | **`spec`, `test-payloads`, `healthcheck`, `liveness`, `readiness`, `mesh`, `invoke`, `report`, `ping`** | ❌ |
| **Mesh wire topics** | **`mesh:register`, `mesh:heartbeat`, `mesh:traces`, `mesh:issues`, `mesh:report`, `mesh:aggregate`, `mesh:dispatch`, `mesh:topology`, `mesh:annotations:add`, `mesh:query:{fleet,service,topic,trace,correlation}`** | ❌ |

The sharpest illustration: a Benzene service exposes its spec at **`/benzene/spec`** but on the
topic plane the same capability is the bare topic **`spec`**. One concept, two conventions, decided
by which transport you happened to arrive on.

---

## The proposed principle

> **Where Benzene puts a name into a namespace it shares with someone else, that name is marked as
> Benzene's. Where the namespace is already Benzene's own, it is not.**

The discriminator is **namespace ownership**, not "is it ours". This is what makes the rule
predictable rather than a blanket prefix-everything reflex:

**Shared namespace → mark it.**
- **Topic ids** share a namespace with the application's own topics (`order:create` sits beside
  `spec`).
- **Headers / message attributes** share a namespace with the application's and the transport's
  (an SQS attribute called `topic` sits beside whatever the app already puts there).
- **Keys embedded in the user's payload** (`_benzeneHeaders` inside an EventBridge `detail`) sit
  literally inside someone else's object.
- **HTTP paths** share a namespace with the application's routes — already handled by `/benzene/`.

**Our own namespace → don't mark it.**
- **Fields of the Benzene envelope** (`topic`, `headers`, `body`, `status`, `detail`). The envelope
  is wholly Benzene's; prefixing inside it is stutter (`benzeneTopic` within a Benzene envelope
  tells the reader nothing new). **This is why the envelope's `topic` field stays `topic` while the
  header becomes `benzene-topic` — they are not the same name in the same place.**
- **Status vocabulary** (`not-found`, `validation-error`) — values within a Benzene-owned field.

**Not ours → never rename.**
- `traceparent`, `tracestate` (W3C Trace Context), `content-type` (HTTP/MIME), `x-correlation-id`
  (de-facto industry convention). Benzene borrows these; renaming them would break the interop
  that is the entire reason for using them. This is an explicit carve-out, not an oversight.

### Format rules

- **Topics:** `benzene:` prefix, `:` as the namespace separator (the existing convention —
  `mesh:query:fleet`), segments lowercase-kebab-case. → `benzene:spec`, `benzene:healthcheck`,
  `benzene:mesh:query:fleet`.
- **Headers / attributes:** `benzene-` prefix, lowercase-kebab-case (matches `benzene-status`).
- **JSON fields:** camelCase, unprefixed inside Benzene-owned objects (unchanged).
- **No configurable alternatives.** A wire name that can be reconfigured is an interop hazard: two
  conformant services that cannot talk to each other, and a conformance matrix that multiplies by
  every knob. One fixed name per concept.

---

## What the principle decides

### §3c — the `topic` header → **`benzene-topic`**

Rename. It is Benzene's name in a shared namespace (SQS/SNS attributes), and bare `topic` is among
the most collision-prone words available on a queue an application also uses. **Reject the
"give users a choice" option** — see the no-configurability rule above.

The envelope's `topic` **field** is unchanged (own namespace).

### §3f — `benzene-version` → **keep the prefix**

The principle answers this directly: it is a Benzene-invented header in a shared namespace, so it
stays marked. A bare `version` is both more collision-prone and ambiguous (version of the payload
schema, the service, or the protocol?).

**Separately** — and *not* decided by this principle — the name could be sharpened to say what it
versions (e.g. `benzene-payload-version`). It is documented as "Draft, not yet implemented", so
that rename is **free today and never again**. Recommend taking that decision now, in task #30, while
it costs nothing.

### §4 — reserved topics → **all prefixed**

`benzene:spec`, `benzene:test-payloads`, `benzene:healthcheck`, `benzene:liveness`,
`benzene:readiness`, `benzene:mesh`, `benzene:invoke`, `benzene:report`, `benzene:ping`, and the
mesh family as `benzene:mesh:*`.

Two independent justifications, both already on record:
1. **Collision.** `report`, `invoke`, `spec` and `ping` are words an application could plausibly
   own. This is the maintainer's stated reason.
2. **Machine-checkability.** The mesh UI's utility filter currently hardcodes a literal list of
   these names *because* they are unpredictable, and its own comment says a uniform prefix would
   collapse it to a single prefix test. Any tool that needs "is this Benzene's?" today must carry a
   list that goes stale; under the principle it becomes `startsWith("benzene:")`.

### `_benzeneHeaders` — **keep as-is**

It satisfies the principle already (marked, in the most hostile shared namespace there is — inside
the user's own payload object). Its different *form* is justified: it is a JSON field, so camelCase
per the JSON convention, and the leading underscore is the conventional "reserved, not yours"
marker in a payload. Renaming it to `benzene-headers` would violate the JSON casing rule to gain
nothing. **Note the inconsistency deliberately in the spec** so it reads as a decision rather than
an oversight.

---

## Counter-arguments, weighed

**"It's verbose."** `benzene:mesh:query:fleet` is 25 characters. These are machine identifiers on
wire formats that already carry trace ids and JSON envelopes; they are not typed by hand in a hot
path. The application's own topics — the ones developers actually write daily — are untouched.
Not decisive.

**"It churns a spec people may have read."** True, and it is the real cost. But the spec is
`DRAFT v0.1`, 1.0 is untagged, and the alternative is shipping the inconsistency permanently: after
1.0 this becomes a major-version migration with a compatibility-shim story. The churn is never
cheaper than it is this month. Not decisive against.

**"`spec` and `healthcheck` are conventional names; prefixing makes Benzene look proprietary."**
The strongest objection, and it is why the carve-out matters: Benzene does **not** rename things it
borrows. But `spec`/`healthcheck` as *Benzene topic ids* are not an industry standard — they are
Benzene's own routing keys, and the profile already namespaces the same concepts as
`/benzene/spec` and `/benzene/health` without anyone finding that proprietary. Not decisive.

**"Two names for one concept is confusing"** (envelope `topic` vs header `benzene-topic`). A fair
hit, and the one place the principle costs clarity. The alternative — prefixing envelope fields —
is worse (stutter, and a bigger breaking change to the most-used shape in the library). Mitigate
with an explicit sentence in `wire-contracts.md` rather than pretending it isn't there.

---

## What this does *not* decide

- **Migration mechanics** — clean break at 1.0 vs. a transition period accepting both ids. (Task
  #29. Provisional view: clean break, because a shim is permanent complexity bought to avoid a
  one-time cost during a window when there are few enough users for that cost to be small.)
- **Whether `benzene-version` gains precision** (`benzene-payload-version`) — task #30, and worth
  taking while it is free.
- **Header tiering** (which rows are contract vs add-on) — task #30, orthogonal to naming.

## Blast radius (for the tasks that follow)

Reserved-topic constants (`Benzene.Schema.OpenApi/ReservedTopics.cs`, `Constants.cs`), every mesh
wire topic and its collector handler registrations, the SQS/SNS topic attribute key, the
conformance fixtures under `docs/specification/conformance/`, `wire-contracts.md` +
`cloud-service-profile.md` + `mesh.md`, the Go port, the mesh UI's `isUtilityTraffic` filter, the
example services, the templates, and any deployed service's declared contract.

---

## Recommendation

**Adopt the principle as stated**, with the three consequences (§3c rename, §3f keep-prefix, §4
prefix-all) and the two carve-outs (Benzene-owned namespaces, borrowed standards). Ruling needed
before task #29 can start.
