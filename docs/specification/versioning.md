# Versioning

**Core — an optional capability** ([core-concepts.md](core-concepts.md) §10): a port need not offer
versioning, and one that does offers it in exactly this shape.

A topic's request and response shapes change over a service's life. Versioning lets a producer keep
sending a shape the topic still accepts, and a service keep serving it, without releasing every
producer and consumer together. A message names its version; the service either has a handler for
that version or can cast the payload to one it has.

## 1. Version is metadata

The version travels beside the payload, never inside it. The payload is exactly what the handler's
type serializes to, in whatever format was negotiated — a version field embedded in the body would
work for one format only. So the version is read from the message's metadata, and versioning stays
independent of serialization.

## 2. One header

The version is the `benzene-version` header ([wire-contracts.md](wire-contracts.md) §2). It is one
name, configurable in one place, and the same name is read inbound and written by outbound clients.

- **No fallback list.** Many producers already send a `version` or `x-version` header for their own
  purposes — an API or SDK version, not a payload version. Reading those as the version would
  silently send such a message to another version's handler, so a bare `version` header is an
  ordinary header and selects nothing. An application whose producers use a different name
  configures that one name instead.
- **HTTP route sugar.** On HTTP a route segment MAY carry the version (`/v2/orders`); the binding
  supplies it exactly as if `benzene-version` had been sent. Where the matched route has no version
  segment, the header is read. The segment's parameter name is the port's business.
- **One header drives both mechanisms.** The same value selects the handler (§4) and names the
  payload's version for casting (§5).

## 3. Absence

A message with no version goes to the topic's **default handler** — the unversioned handler, or
else the one the application declared as the default ([core-concepts.md](core-concepts.md) §2) — and
its payload is taken to be in that handler's version, so no cast applies. Declaring the oldest
version a topic still accepts as its default is what lets producers that predate versioning keep
working once versioning is turned on.

## 4. Dispatch

Several handlers MAY be registered for one topic, one per version. Selection is
[core-concepts.md](core-concepts.md) §2's: the handler for exactly the message's version; otherwise
a handler the version can be cast to (§5); otherwise `not-found`, naming the topic and the version.
There is no "nearest" or "highest available" fallback by default.

## 5. Payload casting

A **cast** turns a payload of one version into another version's type. With casts registered, one
handler can serve producers on several versions:

- **Request.** When the message's version differs from the selected handler's, the payload is cast
  from the message's version to the handler's request type before the handler runs. The handler
  only ever sees its own type.
- **Response.** By default the response is cast back to the request's version, so a producer on an
  older version receives the shape it understands. A port MAY let the application replace that
  default.
- **Degradation.** A topic with no casts registered behaves as an unversioned topic does: exact
  selection only, nothing cast, no error.

*(Informative.)* Casts compose: a port that can chain registered casts (v1→v2, v2→v3) can serve v1
without a direct v1→v3 cast, and a shorter registered chain is preferred over a longer one.

## 6. Absorbing a version difference *(informative)*

There are three ways to absorb a change, and a service may combine them:

- **A versioned handler** (§4) for a genuine behaviour change — the old version keeps its own code,
  usually thin, because the divergent part is the shape and its mapping, not the business logic.
- **A payload cast** (§5) for a compatible change of data shape, with one implementation.
- **A second release deployed alongside the first**, for a change too large or risky for either: a
  rewrite, an independent rollback, a gradual cutover. The mesh can tell the two releases apart
  through the [Service Versions](service-versions.md) module (Provisional); routing a message to a
  particular release is a proposal, not yet specified.
