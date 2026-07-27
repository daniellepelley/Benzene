# Benzene Mesh UI — canonical source

`mesh-ui.html` in this directory is the **one, canonical, cross-language Benzene Mesh UI**: a
single self-contained web page for reviewing a Benzene estate (what each service does, the topics
it consumes/produces, payload schemas, versions, contract drift, topology, usage, and health) from
the language-neutral mesh catalog artifacts every conforming aggregator produces.

- **What it is for, its data contract, its functional requirements, and how each language port and
  the website vendor a copy of it** are documented in
  [`docs/guides/mesh-ui.md`](../docs/guides/mesh-ui.md). Read that first.
- **It is self-contained**: inline CSS/JS, no CDN, no build step, no network calls except the
  relative mesh JSON artifacts (`manifest.json`, `services/*.json`, `topics.json`,
  `topology.json`, `usage.json`, `annotations.json`) it renders. Statically hostable.
- **Reference fixtures** that show the exact artifact shapes live in
  [`website/demos/mesh/`](../website/demos/mesh/); the website demo (`index.html` there) is a
  vendored copy of this file.

## Do not fork

Every consumer (each language port's mesh-UI asset, the website demo) vendors a **verbatim copy**
of this file with the provenance banner intact — same discipline as the conformance fixtures, and
the reason git submodules were rejected. Fixes and features land **here** (with
`docs/guides/mesh-ui.md` updated in the same change); consumers re-vendor. A copy that has drifted
from this file is a bug.

To render it locally, open `mesh-ui.html` next to a `manifest.json` and its sibling artifacts (or
copy the `website/demos/mesh/` fixtures alongside it).
</content>
