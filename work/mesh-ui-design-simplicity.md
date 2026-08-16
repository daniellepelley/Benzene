# Mesh UI — the case for simplicity, and the principle to design to

**2026-08-16.** Written after Wave E shipped, against the observation that the functional advances are
landing and the experience is not. This is a design diagnosis and a direction, not a backlog. The
three design agents created alongside it (`mesh-ux-designer`, `mesh-visual-designer`,
`mesh-ux-critic`) are the mechanism for turning it into one.

---

## The diagnosis

**The product is not too complicated. It is too flat.**

Seven rounds of user testing produced one of the most disciplined honesty models I have seen in a
tool of this kind: the third state at every grain, no claim of safety, a date never without its age,
two measurement planes never summed, a caveat wherever its claim is. Every one of those rules is
right and every one is load-bearing.

**But each was implemented by adding a sentence.** A finding arrived, a sentence answered it, the
round closed. Nobody was ever accountable for what the screen looked like after the thirtieth
sentence — because no round ever asked. The personas were asked *"could you answer your question?"*,
never *"what did that cost you to read?"*

So the product now says a great many true things at the same volume, and leaves the reader to rank
them. Ranking is the work the product exists to do.

### What that looks like, counted

| | |
| --- | --- |
| Bands stacked on the landing page | **11** |
| Of those, one-line amber/red paragraphs a reader must tell apart | **4** |
| Cards on the service page | **6** |
| Pages in the product | **9** |
| Destinations in the nav | **4** |
| Distinct class families for a note / caveat / hint / absence | **~24** |
| Component class families in the stylesheet | **63** |
| Elements whose meaning exists **only** in a `title` attribute | **52** (344 words) |

The last row is the sharpest. This product's readers share evidence by **screenshot** — into an
incident channel, into a steering pack. 344 words of meaning fall out of the page on the way, along
with everything a touch user or a screen-reader user would need. The product owner already ruled on
one instance of this (*"I cannot hover in a room"* — the `†` on the Value page); it is a class, not
an instance.

### The four banners are the whole problem in miniature

Stacked, in order, on the first screen anybody opens:

- feed artifacts that could not be read
- services in the manifest never heard from
- services reporting to the collector and absent from the manifest
- services declaring healthy and then going quiet

Four different diagnoses, four different fixes, four different owners. Rendered as four
near-identical one-line paragraphs with a coloured glyph. Each one was a correct, hard-won fix for a
real finding. Together they are noise: a reader who sees all four reads none of them, and a reader
who sees one cannot tell which of the four it is without reading to the end of the sentence.

**They are all instances of one thing** — *the declared estate and the observed estate disagree, and
here is how.* That is one block with N rows, not four bands.

---

## The principle

The temptation is "say less". That would undo six waves of work and is the one move actually
forbidden: the honesty rules stay, whole.

The way out is that **the rules constrain what must be said, not how loudly.** Nothing in "a caveat is
stated wherever its claim is stated" requires the caveat to be the same size as the claim, in the same
paragraph, above the fold. That was a convention, not a requirement, and it is the convention that is
costing the product its legibility.

> ### Say the verdict at full volume, the qualifier at half, and the derivation on demand.
>
> Three volumes, assigned deliberately, on every surface.

- **Verdict** — the thing the reader came for, stated as a conclusion. `payments-api owes a move on
  payment:capture.` One per surface, at the top, unmissable.
- **Qualifier** — what bounds that conclusion. Visible, always, never hover-only, never hidden behind
  a click: the window, the instance caveat, the plane it came from. Half the weight, adjacent.
- **Derivation** — how the product got there. On demand: an expander, a drill-in, a footnote. Present,
  never in the way.

Today almost everything is rendered at one volume, which is why the page reads as fog. The whole
design programme is assigning the three volumes correctly, surface by surface — and it takes almost
nothing away.

### And the corollary that actually shrinks things

> **A qualifier repeated on four surfaces is evidence that the fact has the wrong home.**

The polled-instance caveat is stated on four screens. The usage window is stated on three. Each
repetition is honest and each was the right local fix. Collectively they are a signal that these are
*properties of a feed*, not of the surfaces that read it — and a product that states its feeds' shape
once, well, in one place, can reference it everywhere else in three words.

---

## The moves, ranked

Ordered by reading removed per day of work. Deletion first, as it should be.

**1 — Delete discussion/annotations. It was already approved and never executed.**
Maintainer decision of 2026-08-16, recorded at `mesh-ui-product-vision.md:1680`: *"discussion/
annotations is removed."* It is still in the product: a card on the service page, a section on the
topic page, `Thread`, `Composer`, a store slice, two `MeshApi` members. Removes a band from two of the
three most-used screens and settles a question every reader currently has to ask ("am I supposed to
use this?"). The plan already exists, including its sequencing constraint about broken intermediate
states. Zero design debate; it is the free win.

**2 — Collapse the four exception banners into one block.**
One heading — *Declared and observed disagree* — and N rows, each: mark, class, the names, the one-line
diagnosis. Four bands become one. The classes become *distinguishable*, which they are not today, and
a reader with none of them sees one absent block instead of four absent ones.

**3 — Give the estate page a verdict line, above everything.**
The first thing on the first screen is currently a five-tile KPI strip: five numbers and no sentence.
There is nowhere the product says what state the estate is in. One line, full volume, before the
tiles. That single change is most of the five-second test.

**4 — Move Topics and Topology off the landing page.**
They are reference surfaces, not answer surfaces. Being collapsible does not make them free — a
collapsed section still costs a band, a heading and a decision. They belong on their own route.
Landing page: 11 bands → 5.

**5 — Fix the navigation: 4 destinations for 9 pages.**
Issues are reachable only via a *see all* on the estate page. Topics have no destination at all.
*Value* is the product's internal word for the page, not the reader's. A nav that omits the second
most-visited surface is why readers report they cannot get back to where they were.

**6 — Design the absence grammar.**
Five kinds of absence — empty, unwired, not-measured, not-yet, could-not-read — arrive as five similar
grey paragraphs. They have five different next actions. Absence is *most of what an honest instrument
renders*, so this is the highest-leverage visual change in the product: a shared, distinct mark that
tells them apart before a word is read.

**7 — Rule on the 52 hover-only tooltips, one at a time.**
Each is a decision: promote to visible, or delete. Nothing load-bearing stays in a `title`. The
product's evidence travels by screenshot, and a screenshot has no hover.

**8 — Replace repeated inline caveats with a marker and a visible footnote.**
Satisfies *stated wherever the claim is stated* at a fraction of the reading, and unlike a tooltip it
survives print, touch and a screen reader. Do this only after move 7 sets the rule, or it becomes the
53rd tooltip.

---

## What must not happen

- **No honesty rule is traded for whitespace.** The third state, the date/age rule, the safety
  prohibition and the two-plane separation all stay, on every surface, enforced by the tests that
  already exist. If a proposed simplification needs one of them relaxed, the proposal is wrong.
- **No qualifier moves into a hover.** Half volume, not zero.
- **No status colour is spent on decoration.** Red, amber and the unknown tone mean what they mean
  everywhere in this product, and a fifth use of amber for emphasis costs the other four their
  meaning.
- **No restyling in place of restructuring.** Four banners that look better are still four banners.

---

## How this gets decided

Three agents, mirroring how the product rounds already work — proposers, then an independent grader,
then the product owner adjudicates:

- **`mesh-ux-designer`** — information architecture, hierarchy, flow, and the words. Owns simplicity;
  has standing authority to propose deletion and is required to propose at least one per assessment.
- **`mesh-visual-designer`** — the system: type, space, colour semantics, density, component
  vocabulary, and the grammar of the five absences. Owns consistency.
- **`mesh-ux-critic`** — grades and never designs, so the designers cannot mark their own homework.
  Seven tests, scored, with the five-second test first and accessibility treated as a test rather than
  a checklist.

The one thing to change about how rounds have been run: **the personas were never asked what the
screen cost them to read.** They were asked whether they could get their answer, and they usually
could — after eleven bands. Both questions matter, and only one has been asked seven times.
