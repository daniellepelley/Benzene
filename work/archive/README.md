# work/archive

Dated records and superseded documents for the specification repository. **Nothing here is current —
do not cite any of it for status.**

Kept because superseded reasoning is worth having when the same question comes round again, and
because archiving is not deletion (rule 6 in [`../README.md`](../README.md), which states the
living-vs-dated rules in full). A document lands here when it is a record of one moment rather than
something anyone still owns and keeps true, and it carries its date in its filename.

The sibling archive in
[`benzene-dotnet/work/archive/`](https://github.com/daniellepelley/benzene-dotnet/tree/main/work/archive)
holds the .NET port's dated records; this one holds the cross-language ones.

## What is here, and why

- `error-payload-proposal-2026-07-25.md` — the July investigation into whether Benzene needed a
  better error payload, and which standard to follow. **Archived 2026-08-20 because it was ruled
  against.** It recommended a "problem-details-*inspired*, explicitly not RFC 9457" shape and
  recorded full RFC 9457 as rejected; the maintainer adopted full RFC 9457, which shipped in
  `b732a74`. Successor: [`docs/specification/wire-contracts.md` §1.3](../../docs/specification/wire-contracts.md#13-problem-details-payload),
  pinned by `docs/specification/conformance/problem-details-cases.json`. The document's §0 records
  the outcome and why the verdict differed; its findings (§2) are what drove the change and are
  still worth reading, but everything from §3's verdict table onward is superseded.
