# Conformance Fixtures

**Status: DRAFT v0.1**

Language-neutral test fixtures for the contracts in [wire-contracts.md](../wire-contracts.md) and
the behaviors in [core-concepts.md](../core-concepts.md). Every Benzene implementation runs the
same fixtures through its own runner; an implementation that passes them (plus the live-interop
tests described in [porting-guide.md §3](../porting-guide.md#3-conformance-testing)) is conformant.
API shape is explicitly not part of conformance.

The .NET runner lives at `test/Benzene.Conformance.Test/` and is the reference for how a runner
consumes these files.

## Fixture files

| File | Verifies |
|---|---|
| `status-vocabulary.json` | The status vocabulary strings and their success/failure classification (wire-contracts §3) |
| `http-status-mapping.json` | Benzene→HTTP and HTTP→Benzene status tables (wire-contracts §4.1) |
| `grpc-status-mapping.json` | Benzene→gRPC and gRPC→Benzene status tables (wire-contracts §4.2) |
| `envelope-cases.json` | End-to-end message envelope handling: request in, pipeline + canonical handler, response envelope out (wire-contracts §1, core-concepts §4–6) |
| `mesh-descriptor-cases.json` | ServiceDescriptor derivation from the canonical handlers, including payload schemas and descriptorHash properties (mesh §2) — required for ports that implement mesh |
| `mesh-trace-cases.json` | TraceEvent behavior: traceparent join/reject rules and the invocation→semantic-status mapping (mesh §3) — required for ports that implement mesh |
| `mesh-collector-cases.json` | Collector ingest, validation, derivation, and degradation behavior (mesh §4–6) — required for ports that implement a collector |

The mesh fixtures apply only to ports that implement the optional mesh module (mesh.md §7);
a port without mesh skips them and remains conformant.

## Canonical handlers

Envelope cases run against a fixed set of handlers that every runner MUST register natively,
with exactly these topics and behaviors:

| Topic | Request body | Behavior |
|---|---|---|
| `conformance:greet` | `{ "name": string }` | Returns `Ok` with payload `{ "greeting": "Hello <name>" }` |
| `conformance:status` | `{ "status": string, "errors": string[]? }` | Returns the given status verbatim. For a success-class status, the payload is `{ "applied": "<status>" }`; for a failure-class status, the result carries the given `errors` (and no payload). |

No handler is registered for any other topic — cases targeting unregistered or empty topics
verify the router's `NotFound` / `ValidationError` behavior.

## Envelope case format

```json
{
  "name": "unique-case-name",
  "request":  { "topic": "...", "headers": { }, "body": "..." },
  "expected": {
    "statusCode": "Ok",
    "body": { "greeting": "Hello world" },
    "headers": { "content-type": "application/json" }
  }
}
```

- `request` is the inbound envelope (wire-contracts §1.1), verbatim.
- `expected.statusCode` is compared exactly.
- `expected.body`, when present, is parsed JSON compared by **subset**: every field in the
  expected object must be present in the actual (parsed) response body with a deeply-equal value;
  extra fields in the actual body (including null-valued ones) are ignored. This is deliberate —
  implementations may enrich responses, and writers may emit or omit null properties
  (wire-contracts §6).
- `expected.headers`, when present, is compared by subset the same way (keys case-insensitive).
- Human-readable message wording (e.g. the `detail` text of router-generated errors) is
  intentionally not asserted.

## Mesh fixture formats

Subset matching is as for envelope cases, with one addition needed by these fixtures:
**arrays compare by exact length with per-element subset matching**, and an expected empty
array (`[]`) matches an actual array that is empty *or absent* (writers may omit empty
collections).

- `mesh-descriptor-cases.json` — the runner registers the two canonical envelope handlers
  (above) natively, builds the service descriptor with the fixture's `serviceInfo`, and
  subset-compares `expectedDescriptor`. `runtime` and the hash value are per-port and not
  pinned; instead `hash` asserts the hash's *properties*: its `sha256:` + 64-hex format,
  invariance to `instanceId`, and sensitivity to `serviceVersion` and to the topic set.
- `mesh-trace-cases.json` — `traceparent` rows assert the join/reject rules of mesh §3
  observationally (a valid header's ids are adopted; an invalid one yields a fresh 32-hex
  trace id and no parent). `invocations` rows run one envelope each through a pipeline with
  the trace middleware installed and the canonical handlers plus one extra canonical mesh
  handler registered:

  | Topic | Request body | Behavior |
  |---|---|---|
  | `conformance:panic` | any | Panics/throws unconditionally — pins the rule that a handler panic is traced as `ServiceUnavailable`, not lost |

  `expectedEvent` is subset-matched against the single TraceEvent exported for that
  invocation. (`conformance:panic` is registered only for these trace cases, not for
  descriptor or envelope cases.)
- `mesh-collector-cases.json` — each case's `steps` run in order against one fresh collector;
  each step is an envelope request/expected pair asserted like an envelope case. The
  `mesh:query:*` responses are asserted as the observable surface for the ingest/derivation
  rules of mesh §4–6; those query shapes are not themselves promoted contracts.

## Mapping table format

```json
{
  "forward": [ { "from": "Ok", "to": "200" } ],
  "reverse": [ { "from": "200", "to": "Ok" } ]
}
```

Each row is asserted independently against the implementation's forward/reverse mapper. Rows with
`from` of `"<unknown>"` assert the mapper's default for an unrecognized input.
