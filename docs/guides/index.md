# Guides

Language-neutral guides to Benzene's concepts and tooling — true for every language port, alongside
the normative [specification](../specification/index.md). Each guide explains an *idea*; how to
*use* it in a particular language is documented in that language's own repo.

- [Code Generation](code-generation.md) — how a Benzene service's self-description drives generated
  API specs, client SDKs, and infrastructure, independently of the language it's written in
- [Mesh UI](mesh-ui.md) — the one cross-language web page for reviewing a Benzene estate (what each
  service does, topics, payloads, versions, drift, topology, usage, health), its data contract, and
  how every language port and the website vendor the single canonical copy
- [Design Principles](design-principles.md) — "opinionated but optional": the adoption ladder, what
  each capability needs and how it degrades, the extension-point catalogue and the shorthand ladder
- [Porting Guide](porting-guide.md) — concept versus idiom, a suggested porting order, and the
  conformance-testing approach for implementing Benzene in another language
- [Port Quality Standards](port-quality-standards.md) — the Definition of Done for a language port:
  the DX-champion workflow and the quality gates a unit of port work clears
