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

## Mapping table format

```json
{
  "forward": [ { "from": "Ok", "to": "200" } ],
  "reverse": [ { "from": "200", "to": "Ok" } ]
}
```

Each row is asserted independently against the implementation's forward/reverse mapper. Rows with
`from` of `"<unknown>"` assert the mapper's default for an unrecognized input.
