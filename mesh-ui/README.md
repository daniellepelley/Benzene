# Benzene Mesh UI — canonical vendored copies

Two self-contained pages, both canonical and both cross-language:

- **`mesh-ui.html`** — the estate view, for reviewing a Benzene estate (what each service does, the topics it
  consumes/produces, payload schemas, versions, contract drift, topology, usage, health, live flows
  and issues) from the language-neutral mesh catalog artifacts every conforming aggregator produces.
- **`mesh-spec-ui.html`** — one service's contract: its topics, HTTP routes, payload schemas and
  examples. The same file serves three homes, because only the fetch differs: `?service=` reads the
  aggregator's stored snapshot, while `?url=`, a host-injected `data-spec-url`, or a `spec.json`
  sitting beside the page render a document directly. `Benzene.Spec.Ui` embeds it as `spec-ui.html`.

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

Edits made here are lost on the next re-vendor and are not covered by any test. To change either page:

1. change `src/` in `benzene-ui`,
2. `npm run build`, then
   `cp dist/index.html build/mesh-ui.html && cp dist/spec/spec.html build/mesh-spec-ui.html`,
3. copy both over the files here and the other vendored copies.

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
| `data-spec-url` | `?url=` | *(spec viewer)* The spec document to render. Defaults to `./spec.json` |
| `data-fleet-url` | `?fleet=` | The wire-envelope endpoint the live plane polls. Absent ⇒ the static floor |
| `data-annotations-url` | `?annotations=` | The annotation write endpoint. Absent ⇒ discussion is read-only |

`MeshUiPage.GetHtml` and `SpecUiPage.GetHtml` inject these by rewriting the literal string
`<html lang="en">`, so that opening tag must survive the build unchanged. benzene-ui's CI asserts it
does, for both pages.
