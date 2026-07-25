# Benzene — Production Provenance (the IRIS lineage)

**Status:** SOURCE OF TRUTH for provenance claims — recorded from the maintainer, 2026-07-25.
Permission and precision gates are UNRESOLVED; see §4 before any of this is published.
**Last Updated:** 2026-07-25
**Purpose:** Record, precisely, what Benzene's predecessor achieved in production, so marketing can
use the strongest trust asset the project has **without overclaiming it**. A framework nobody has
run in production is a hard sell; a design pattern that already runs a flagship product is a
different conversation. The difference between those two sentences is the whole point of this file.

---

## 1. The facts, as stated by the maintainer

- A **forerunner library** — materially the same design (ports/adapters + message handlers +
  middleware pipeline), at a **significantly narrower scope** — was built and used at
  **IRIS Software Group**.
- It went into **IRIS Elements**, a **flagship accountancy platform**, and **runs in production on
  AWS today**.
- The **chief architect at IRIS decided that all new systems should use the precursor**, across the
  IRIS estate. (Maintainer's phrasing was "across the Iris state"; read as *estate*, i.e. their
  software estate. **Confirm the wording before quoting it.**)
- **Adoption spread organically.** It was developed by **one team**, and **other teams adopted it
  because it was materially easier to develop with than the alternatives** — pull, not push. No
  mandate drove the initial spread; the mandate came later.
- **Benzene is the next generation of that design**, with the scope **broadened well beyond AWS
  Lambda**: Azure Functions, Kubernetes, Google Cloud, and anything running Docker.

## 2. What this is evidence *for* — and what it is not

This is the distinction the campaign must hold, every time:

| Defensible | NOT defensible |
| --- | --- |
| The **design** is proven in production, in a flagship product, at a real software company | "**Benzene** is battle-tested in production" — Benzene itself is 1.0, unreleased |
| Proven **within a narrower scope** (AWS Lambda–centric) | Proven across the full multi-cloud surface Benzene now claims — the broadened scope is **new and unproven in production** |
| A chief architect **standardised on the precursor** for new systems | "IRIS uses Benzene" / "IRIS endorses Benzene" — a different and unowned claim |
| Teams adopted it **voluntarily because it was easier** — organic, developer-led spread | Any implication of a commercial relationship, sponsorship, or partnership with IRIS |

**The honest sentence** (subject to §4 permission) is close to:

> "The design Benzene generalises has been running in production on AWS in IRIS Elements, a flagship
> accountancy platform, where it spread team-to-team on its own merits and was then standardised on
> for new systems. Benzene is the next generation of that design, broadened beyond AWS Lambda —
> and that broader surface is new."

Note the last clause. Volunteering the limit is what makes the rest believable, and it is true.

## 3. Why this matters more than any feature claim

The hardest objection a solo-maintainer OSS framework faces is not "what does it do" — it is
**"who has actually run this, and will it exist in three years"**. Every other trust signal
(tests, docs, coverage badges) is self-reported. This one is not.

Two specific marketing consequences:

1. **It answers the trust question directly**, which no amount of feature content can.
2. **The organic-spread story is independent evidence for the DX claim.** "Other teams picked it up
   because it was easier than the alternatives" is exactly the campaign's wedge — *validated by
   people with no stake in it*. Developer-led adoption inside a company is the closest thing to a
   controlled experiment this project will ever get, and it is far more persuasive than the
   maintainer asserting the same thing.

## 4. Gates — resolve BEFORE any of this is published

Marked unresolved deliberately. Publishing any of §1 without these is a real risk, and the
reputational cost of getting it wrong is far higher than the benefit of shipping it a week earlier.

1. **Permission to name IRIS Software Group / IRIS Elements.** Naming a company (especially a
   current/former employer) in promotional material without consent risks the relationship and
   potentially more. **Get explicit sign-off from someone empowered to give it**, in writing.
   - **Anonymised fallback** if permission isn't given or isn't quick: *"a flagship accountancy
     platform at a UK software group, in production on AWS"*. Weaker, still true, still useful —
     and it costs nothing to use while permission is pending.
2. **IP / clean-lineage check.** If the precursor was built as work-for-hire, its **code** is not
   the maintainer's to reuse, even though the **design pattern** is not ownable. Be certain Benzene
   is an independent implementation before advertising the lineage — publicising the connection is
   exactly what would invite the question. Confirm, don't assume.
3. **Precision of the architect's decision.** "All new systems should use it" is a strong claim.
   Confirm the actual scope and current status before it appears in copy, and prefer a **direct
   quote with attribution** over a paraphrase.
4. **The specifics that make it credible.** Vague provenance reads as embellishment; specifics read
   as fact. Gather what can be shared: how long in production, how many teams/services, rough
   traffic scale, and the year adoption started.

## 5. The prize, if permission lands

A **named quote from the IRIS chief architect** would be the single most valuable marketing asset
available to this project — worth more than any blog post, and worth being patient for. Ask for it
explicitly rather than hoping it emerges: a two-sentence quote on why they standardised on the
approach, cleared for public use.

If a full case study is possible, that is the strongest launch artefact there is. If not, one
cleared sentence still changes the campaign.

## 6. Open questions for the maintainer

- Was the maintainer the author of the precursor at IRIS, and in what capacity?
- Who is empowered to grant permission, and is the relationship warm enough to ask?
- Is IRIS still actively using it, and is the architect's standardisation decision still current?
- Any confidentiality obligations that touch on this at all?
