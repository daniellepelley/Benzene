# Service Versions

**Module — Provisional: .NET.** Optional; a collector without it stays collector-conformant
(mesh.md §4), because a descriptor that omits `serviceVersion` keys exactly as it always has.

A service's identity in the mesh is its **name**, which says *which* service but not *which release
of it is running*. When two releases are deliberately deployed side by side — a rewrite, a risky
behaviour change, a gradual cutover — the mesh must be able to tell them apart rather than see one
service that keeps changing its mind. This module adds that layer: a service version is an identity
(§1) and, when declared with a scheme, an order (§2). Routing a message to a particular release is
not specified; the proposal is `work/side-by-side-service-versions.md` in the Benzene repo.

The collector rules that use the identity — the catalog keyed by `(service, serviceVersion)`,
re-registration scoped to one version, graph membership by service — are in [mesh.md](mesh.md) §4.
Pinned by `conformance/mesh-service-version-cases.json` (identity) and
`conformance/mesh-version-order-cases.json` (order).

## 1. Identity

`service` identifies *which* service; `serviceVersion` identifies **which immutable release of it is
running**. A collector's catalog key is the pair `(service, serviceVersion)`, so two releases
deployed side by side are two catalog entries rather than one silently overwriting the other.

A service version is an **entity, not a shape**. It *has* a contract — the `topics`, `produces` and
schemas frozen for that release — but is not *defined by* one: two releases MAY declare
byte-identical contracts and still be different versions, because a rewrite, a corrected calculation
or a swapped downstream dependency changes what a service *does* without changing what it
*declares*. Any scheme that derives version identity from contract content collapses exactly the
case this layer exists to distinguish.

**Identity is extrinsic**, and resolves in this order:

| | Source | Status |
|---|---|---|
| 1 | A non-empty `serviceVersion` on the descriptor | The identity. Operators SHOULD declare one per release; it is the only source that is both stable and human-meaningful. |
| 2 | An immutable per-release identifier the substrate assigns — a published AWS Lambda version, a Kubernetes ReplicaSet, a Cloud Run revision, an Azure deployment slot | A port MAY read one as a fallback. Which (if any) exists is per-platform, so this is **informative**, read the same way `placement` is. |
| 3 | Neither | The service has **exactly one** service version. This is not an error and MUST NOT be reported as one. |

A port MUST NOT synthesize the identity from a value that varies independently of releases — a
process start time, a per-boot random id, an `instanceId`. Those mint a fresh version per replica or
per restart, shattering one release into phantom siblings. `descriptorHash` ([mesh.md](mesh.md) §2.2) is likewise not an
identity: it fingerprints the contract, which is precisely what case 2 exists because the contract
cannot supply.

Case 3 cannot be improved on. Two replicas of one deployment, and two side-by-side releases with
identical contracts, are the same observation to a collector: several instances reporting one service
name and one contract. Without an extrinsic identifier there is nothing to tell them apart, and a
collector that claimed otherwise would be inventing a distinction the data does not contain.

**Immutability.** Once `(service, serviceVersion)` has registered a contract, that pair's contract
does not change. A re-registration of the same pair with a different `descriptorHash` is contract
drift ([mesh.md](mesh.md) §5) — the declared version is an assertion two builds disagree about. Two *different* versions
reporting different hashes is **not** drift; it is the expected state of a side-by-side deployment.

**Backward compatibility.** A descriptor that omits `serviceVersion` keys exactly as it always has:
case 3 gives it one service version, so a collector implementing this section and one predating it
agree on every single-deployment estate. Only a descriptor that declares a version exercises the new
behavior — which is why this section is additive rather than a breaking change to the existing
keying of [mesh.md](mesh.md) §4.

## 2. Order

§1 makes a service version an identity. This section makes a *declared* one **orderable**, so a
reader can say which of two releases of one service is later.

Order is what separates a difference from a direction. Without it a tool comparing two releases can
only report that they differ; with it the same comparison reports an **upgrade** or a **rollback**,
which is the question anyone planning a deployment is actually asking. It is also what lets a reader
say "four versions behind" and what makes "the newest version of this service" mean anything.

**A declared `serviceVersion` MUST carry a `versionScheme`** naming the comparison rule, from exactly
this closed set:

| `versionScheme` | Value form | Comparison |
|---|---|---|
| `integer` | one or more ASCII digits, no sign, no separators | numeric, by value |
| `semver` | a Semantic Versioning 2.0.0 version | SemVer precedence, including pre-release rules |
| `lexicographic` | any non-empty string | codepoint-wise ordinal comparison |

The scheme is **declared, never inferred from the value.** `"10"` and `"9"` order one way as integers
and the opposite way as strings, so a port that guesses will disagree with a port that guesses
differently — about which release is newer, in a tool used to decide deployments. There is no default:
a descriptor declaring a `serviceVersion` without a `versionScheme` is declaring an identity, not an
order: §1 case 1 still keys it, and it is simply not orderable.

A `serviceVersion` that does not parse under its declared `versionScheme` is **invalid** and MUST be
rejected at the point of declaration rather than carried and compared later.

**Ordering is defined only within one `service`.** Comparing versions of two different services is
meaningless and MUST NOT be offered; there is no global version line.

**Order is not lineage.** It says which version is *later*, never which *contains* the other. A hotfix
`1.2.4` cut from a release branch while trunk is on `1.3.0` orders correctly and is an ancestor of
nothing. A port MUST NOT present a later version as superseding, including, or being safe to replace
an earlier one on the strength of order alone.

**Two versions of one service with different schemes are NOT ORDERABLE**, and a port MUST report that
outcome rather than choosing one scheme or falling back to another comparison. A service that switched
schemes — build numbers to SemVer — has a real discontinuity in its history, and inventing an order
across it would be a claim no data supports. `not-orderable` is a normal outcome, not an error.

**`createdAtUtc` is not a substitute and not a tiebreak.** Build time and version order are different
facts and MUST be kept apart: build timestamps go backwards in practice, through rebuilt artifacts,
clock skew and pipelines finishing out of order. Where a port holds both and they **disagree** — a
later version built earlier — that disagreement SHOULD be surfaced. It means an out-of-order pipeline,
a rebuilt artifact or a backdated tag, each of which is worth knowing.

**Relationship to §1's identity sources.** Ordering attaches to source 1 — a declared
`serviceVersion` — only. §1's source 2, an identifier the substrate assigns, is orderable for some
platforms and not for others: a published AWS Lambda version is integer-like, a Kubernetes ReplicaSet
name is a hash. A port MAY treat a substrate identifier as `integer` where the platform documents it
as monotonic, and MUST otherwise treat it as carrying no order. This is why §1's *"operators SHOULD
declare one per release"* matters more than it appears to: an undeclared version costs a reader the
timeline, the direction and the newest-version query, not merely a readable label.

Pinned by `conformance/mesh-version-order-cases.json` — required only for ports that order service
versions. A port implementing §1 without §2 stays service-version conformant.

## 3. Degradation

A descriptor with no `serviceVersion` and no substrate identifier is one service version (§1,
case 3): single-deployment estates behave exactly as they would without this module. A collector
that does not implement it keys by `service` alone, so two side-by-side releases overwrite each
other's registration in it — the reduced behaviour this module exists to remove, not an error.
