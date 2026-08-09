# Benzene Mesh UI — canonical vendored copy

`mesh-ui.html` in this directory is the **one, canonical, cross-language Benzene Mesh UI**: a
single self-contained web page for reviewing a Benzene estate (what each service does, the topics
it consumes/produces, payload schemas, versions, contract drift, topology, usage, and health) from
the language-neutral mesh catalog artifacts every conforming aggregator produces.

- **What it is for, its data contract, its functional requirements, and how each language port and
  the website vendor a copy of it** are documented in
  [`docs/guides/mesh-ui.md`](../docs/guides/mesh-ui.md). Read that first.
- **It is self-contained**: inline CSS/JS, no CDN, no network calls except the relative mesh JSON
  artifacts (`manifest.json`, `services/*.json`, `topics.json`, `topology.json`, `usage.json`,
  `annotations.json`) it renders. Statically hostable.

## This file is a build output — do not edit it

As of 2026-08-09 it is the built artifact of
[**benzene-ui**](https://github.com/daniellepelley/benzene-ui), a React + TypeScript component
library. It was previously a hand-maintained 5,000-line page with 595 top-level variables and no
tests; the componentised source has ~180 tests over the store, a Storybook per component, and
generated types pinned to the mesh contracts, and it builds to a smaller page than the one it
replaced.

Edits made here are lost on the next re-vendor and are not covered by any test. To change the UI:

1. change `src/` in `benzene-ui`,
2. `npm run build && cp dist/index.html build/mesh-ui.html`,
3. copy `build/mesh-ui.html` over this file and the other vendored copies.

The build is deterministic, and benzene-ui's CI fails if a fresh rebuild does not reproduce its
committed artifact byte for byte — so a committed build output cannot drift from its source in
silence.

**Why a committed artifact rather than a build step in each consumer:** the consumers are not
JavaScript projects. `Benzene.Mesh.Ui` embeds this file as a .NET resource; making a NuGet package
build depend on `npm ci` to obtain an HTML file would be a poor trade.

## Do not fork

Every consumer (each language port's mesh-UI asset, the website demo) vendors a **verbatim copy**
of this file — same discipline as the conformance fixtures, and the reason git submodules were
rejected. A copy that has drifted from this file is a bug.

To render it locally, open `mesh-ui.html` next to a `manifest.json` and its sibling artifacts (or
copy the `website/demos/mesh/` fixtures alongside it). **Reference fixtures** showing the exact
artifact shapes live in [`website/demos/mesh/`](../website/demos/mesh/); the website demo
(`index.html` there) is a vendored copy of this file.

## Deployment attributes

A host serving this page from inside a running service can inject three attributes onto the
`<html>` element, all of which the page reads (a query parameter of the same name wins over each):

| Attribute | Query param | What it does |
|---|---|---|
| `data-manifest-url` | `?url=` | Where the artifacts are published; everything else resolves relative to it |
| `data-fleet-url` | `?fleet=` | The wire-envelope endpoint the live plane polls. Absent ⇒ the static floor |
| `data-annotations-url` | `?annotations=` | The annotation write endpoint. Absent ⇒ discussion is read-only |

`MeshUiPage.GetHtml` injects these by rewriting the literal string `<html lang="en">`, so that
opening tag must survive the build unchanged. benzene-ui's CI asserts it does.
