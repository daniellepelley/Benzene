# Claim Check

**Module — Provisional: .NET.** Optional middleware that lets a payload too large for a transport's
message-size limit travel by reference. It is defined by one header, `benzene-claim-check`
([wire-contracts.md](wire-contracts.md) §2).

`benzene-claim-check` is written by an **optional** outbound middleware when a payload exceeds a
configured size threshold, letting a large payload bypass a transport's message-size limit.

- The header's value is an **opaque, URI-form reference** of the shape `scheme://location/key`,
  issued by the sender's payload store (e.g. `s3://bucket/key`, `azblob://container/key`).
- The message **body** of an offloaded message is **unspecified** — a consumer MUST NOT interpret
  it directly, and MUST treat the header as authoritative.
- A consumer with the claim-check add-on wired MUST replace the body with the stored content
  verbatim **before** deserialization; every other header (including `content-type`) applies to
  the **hydrated** body, not the placeholder.
- A consumer MUST resolve a claim-check reference only through its own configured store, and MUST
  fail the message loud — never silently skip it — when the reference cannot be resolved or lies
  outside that store's own configuration. This is a security boundary: a consumer MUST NOT fetch
  an attacker-supplied arbitrary location.
- Deleting the stored payload at read time is **forbidden**. A fan-out transport (e.g. pub/sub) may
  have multiple consumers reading the same offloaded message, and at-least-once redelivery would
  find the blob already gone if the first reader deleted it. Retention is store-side expiry (e.g. a
  lifecycle rule) agreed between the communicating services — not specified further here; that is
  deployment-specific.

A service that offloads is interoperable only with consumers that have wired this module and share
access to the same store — an explicit deployment agreement between the services.
