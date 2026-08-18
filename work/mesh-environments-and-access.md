# Mesh across environments — access, topology, and how the UI is built

**2026-08-18.** Answers three questions asked together, because they turn out to be one question
with a shared blocker. Companion to `work/mesh-ui-aims.md`; that says what the UI is for, this says
who may see it, where it runs, and how it gets built.

Decisions are marked **RULING**. Things that need the security reviewer before code are marked
**OPEN**.

---

## 0. The blocker, first

**The mesh cannot currently say which environment it is looking at.**

`docs/specification/mesh.md` §2 gives `placement.cloud` and `placement.region`. There is no
`placement.environment`. `MeshServiceDescriptor` has no equivalent. The only environment signal
anywhere in the product is `IMeshDispatchEnvironment.IsProduction` — a boolean, read from
`ASPNETCORE_ENVIRONMENT` **inside each service's own process**, never published, never reaching a
screen.

So today a dev mesh and a production mesh render pixel-identically, and a reader can tell them apart
only by the URL in the address bar. Everything below — per-environment policy, an environment
switcher, "am I about to fire this at production" — is unbuildable until this exists.

**RULING E1 — `placement.environment` lands in the spec first.** A free-text, service-declared
label (`"production"`, `"staging"`, `"dev-pr-412"`), configured at deploy time, never inferred by a
port from a hostname. It is **informative**, exactly like `placement.cloud`: absent means unknown,
and unknown must never render as "dev". This was already approved once (product vision §5.6) and
never landed.

Free text, not an enum: every organisation names environments differently, and an enum would force
`dev-pr-412` to be spelled `Development` and lose the thing that made it worth showing.

---

## 1. The reframe: dispatch has two risks, not one

`MeshDispatchGate` allows dispatch when `!IsProduction || AllowInProduction`, and its own
documentation gives the reason as side-effects: *"it invokes a service's real handler with the
supplied payload (real side-effects execute)"*.

That is one risk. The requirement that prompted this names a **different** one: a dispatch **returns
a response payload**, and in production that payload is production data. A support engineer running
`customer:get` against production to resolve a ticket is not causing a side-effect — they are
**reading customer data**, and the mesh is the thing that showed it to them.

| | Risk | Who it threatens | Mitigated by |
|---|---|---|---|
| **W** | The handler executes for real — a payment is taken, an email is sent | The business | Not dispatching; or dispatching only read-shaped topics |
| **R** | The response carries real production data | The data subject, and the operator's compliance posture | Identity, authorisation, and an audit record |
| **D** | The endpoint is abused as an amplifier | Availability | Login, CSRF header, throttle — **already shipped** |

**RULING E2 — W and R are separately governed.** One boolean cannot express "may read, must not
write", which is precisely the support engineer's profile. Conflating them is why the current gate
answers the support question with a flat no.

**This is the argument for mesh access in the first place**, and it should be stated in the product
docs rather than left implicit: giving a support engineer a scoped, audited, single-topic read
through the mesh is *materially less dangerous* than giving them a database credential. The mesh
competes with a `psql` prompt, and it wins on blast radius, on shape (one topic, one payload, not a
whole schema), and on leaving a record.

---

## 2. The access model

Three inputs, not one. The gate today reads one.

```
allowed  =  policy( capability , environment , principal )
```

- **Capability** — what is being attempted. Not one flag: `read-catalogue`, `read-live`,
  `dispatch-read`, `dispatch-write`, `refresh`.
- **Environment** — from `placement.environment` (§0), or the mesh host's own configured label.
- **Principal** — who is asking. **Today this is an email in an allowlist and nothing else.**

**RULING E3 — the session must carry a role, not just an identity.** `OidcSessionPayload` is
`(Email, Exp)`; `MeshOidcOptions` has a flat `AllowedEmails`. There is no way to express "Sam may
dispatch in production, Alex may not", so the requirement cannot be built without this. Roles come
from the IdP (a `groups`/`roles` claim, scope-requested) with a configured mapping to mesh roles,
falling back to a static per-role email list for the small deployments the current allowlist serves.

**RULING E4 — the default policy, which an operator may replace wholesale.** Rows are
capability × environment; cells are the roles permitted.

| Capability | Non-production | Production |
|---|---|---|
| `read-catalogue` — what services declare | any signed-in | any signed-in |
| `read-live` — traffic, flows, issues | any signed-in | any signed-in |
| `refresh` — re-run aggregation | any signed-in | `operator` |
| `dispatch-read` — fire a read-shaped topic | any signed-in | **`support`, `operator`** |
| `dispatch-write` — fire a state-changing topic | any signed-in | **nobody by default** |

Two properties of that table are the point:

- A developer signed into the production mesh can read the catalogue and the traffic, and cannot
  dispatch. That is the "same person, less access in production" case.
- A support engineer *can* `dispatch-read` in production. That is the support-ticket case, and it is
  the row that justifies mesh access existing at all.
- `dispatch-write` in production is off for everyone by default and must be turned on deliberately,
  per role, by an operator who owns the consequence. This preserves what `AllowInProduction`
  protects today rather than weakening it.

**OPEN E5 — how does the mesh know a topic is read-shaped?** `dispatch-read` vs `dispatch-write` is
the load-bearing distinction and the catalogue does not currently carry it. Candidates: a declared
`safe`/`idempotent` marker on the handler (honest, opt-in, service-owned); inferring from an HTTP
mapping of `GET` (available today via `consumers[].httpMappings[]`, but only for HTTP-bound topics);
or the presence of a `response` and absence of `events`. **Inference is the wrong default** — a
mis-inferred "read" that takes a payment is the worst failure this document can produce. Prefer the
declared marker, treat undeclared as write-shaped, and take this to the security reviewer.

**RULING E6 — every production dispatch is audited.** Who, which topic, which environment, when,
and the outcome status. Not the payload and not the response body: an audit record that copies the
data is a second copy of the thing being protected. Without this, "less risky than a database
credential" is an assertion rather than a property.

**RULING E7 — the UI renders the policy, never enforces it.** The server decides; the UI reads what
it is told and disables what is refused, with the reason. A hidden control that a crafted POST still
reaches is the defect this rule exists to prevent — the same rule `UseMeshRefreshGuard` already
follows by sitting ahead of every handler.

---

## 3. Deployment topology

**Today: one mesh per environment.** The mesh host reads artifacts from its own bucket and traces
from its own backend, resolved relative to its own origin (`meshApi.ts:resolveUrl`). Simple, and it
inherits the environment's own network boundary for free — the dev mesh physically cannot reach
production.

**Wanted eventually: one mesh, many environments** — the Datadog model, and the user's own framing
that this is "a lot more away".

**RULING E8 — both are supported; the single-environment deployment stays the default.** It is the
one that needs no cross-account trust, and the one an evaluator can stand up in an afternoon. The
neutral deployment is an addition, not a replacement.

**RULING E9 — the seam is an environment source registry, and it goes in now even for one
environment.** The UI's data layer today resolves every artifact against one base URL. Introducing
*"the estate you are looking at is one of N named sources"* — with N=1 — is a small change now and a
large one later, and it makes the environment visible on screen immediately, which §0 says is
missing regardless of topology.

What the neutral deployment additionally requires, none of it needed for N=1:

1. Per-environment artifact locations and credentials, held by the mesh host, never the browser.
2. Cross-account read access — this is the genuinely hard part and it is an infrastructure problem,
   not a UI one.
3. Policy evaluated against the **selected** environment, not the host's own. A mesh UI deployed in
   production and pointed at dev must apply dev's policy to dev, and production's to production.
4. An environment indicator that cannot be missed, present on every screen and unmistakable at a
   glance — production must not look like dev when both are one dropdown apart.

**OPEN E10 — does the neutral mesh hold read credentials for production?** That concentrates access
in one place, which is either the point or the problem depending on the operator. Both stances are
legitimate; the product should support the federated one (each environment's mesh serves its own
data; the neutral UI aggregates over authenticated calls) rather than assuming central credentials.

---

## 4. How the UI gets built

**RULING E11 — screens are built as independent components with their own data needs, and merged
only when a merge is demonstrably better.** Confirms the direction already taken in
`mesh-ui-aims.md` §3 (one screen, one question) and gives the build-order rule behind it.

The evidence is on the screens themselves. The Service page — independent boxes, each answering one
thing — is the surface that works. The Estate page, built holistically as one composed view, is the
one carrying four near-identical banners and five unexplained numbers. Composition was the thing
that went wrong, not the thing that was missing.

Consequences:

- A new capability gets its own screen first. It earns a place on an existing screen by being needed
  *there*, not by existing.
- A box owns its own empty, loading and refused states. This is what makes per-environment policy
  cheap: a box that is refused in production renders its own refusal, and no other box knows.
- Merging is a deliberate act with a reason, not the default. `#compose` merging into the Test
  Console (aims §3) is a merge with a reason: same job, two doors.

---

## 5. Sequence

Strictly ordered — each step is unbuildable without the one above it.

| | Step | Where |
|---|---|---|
| 1 | `placement.environment` in the spec, fixtures, and the Go reference | `docs/specification/mesh.md` §2 |
| 2 | Ports emit it; the environment is visible on every screen | four port repos, `benzene-ui` |
| 3 | Environment source registry with N=1 | `benzene-ui` data layer |
| 4 | Roles in the session; capability × environment policy server-side | `Benzene.Mesh.Auth.Oidc`, `Benzene.Mesh.Dispatch` |
| 5 | Read/write topic shape (E5) + production dispatch audit (E6) | spec + `Benzene.Mesh.Dispatch` |
| 6 | Neutral multi-environment deployment | new, and only once 1–5 hold |

Steps 1–3 are worth doing regardless of whether the neutral deployment is ever built: they fix the
fact that a reader cannot tell which estate is on screen.

---

## 6. What this does not do

- **No environment-specific *product*.** One UI, one set of aims; the policy varies, the product
  does not. A production mesh that answers different *questions* from a dev mesh is two products.
- **No mesh-owned user directory.** Roles come from the IdP. The static list survives only as the
  small-deployment fallback the current allowlist already is.
- **No approval workflow.** "Request access to run this in production" is a ticketing system's job
  (`mesh-ui-aims.md` §4 excludes incident and ticket management, and this stays excluded).
- **No secret redaction promise.** If a topic returns sensitive data, `dispatch-read` shows it —
  that is what it is for. The control is *who may dispatch*, not what comes back. Claiming
  field-level redaction the mesh cannot enforce would be a verdict the product has not earned.
