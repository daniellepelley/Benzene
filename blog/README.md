# Benzene blog / LinkedIn posts

Draft posts for announcing and explaining Benzene on LinkedIn. Each file is self-contained and
written to be copy-pasted straight into a LinkedIn post (no markdown rendering assumed — plain
text with line breaks, since LinkedIn doesn't render `**bold**` or tables).

Suggested posting order — start with the launch post, then alternate a developer-angle post with
a management-angle post so the series keeps reaching both audiences:

1. `01-introducing-benzene.md` — launch post. Both audiences. What Benzene is, in one read.
2. `02-write-once-run-anywhere.md` — developer angle. The hexagonal pipeline, with a code sample.
3. `03-the-lock-in-tax.md` — management angle. The business case: portability, hiring, risk.
4. `04-honest-abstraction.md` — both audiences. Why Benzene doesn't hide AWS/Azure behind a
   generic interface, and why that matters (the anti-Dapr position).
5. `05-stop-copy-pasting-cross-cutting-concerns.md` — developer angle. Middleware pipeline reuse
   (correlation IDs, retries, tracing, validation) as a concrete daily pain point.

Notes for whoever posts these:

- Benzene is pre-1.0 (currently shipping `-alpha` NuGet packages). The posts say this plainly —
  don't strip that out to sound more finished than it is; it's consistent with the project's own
  "honest by design" positioning and reads better on LinkedIn's technical audience than a
  polished-sounding claim that doesn't hold up on inspection.
- Repo: https://github.com/daniellepelley/Benzene — swap in the real URL/handle before posting if
  this differs.
- Each post ends with a light CTA (star the repo / follow / try the quickstart). Adjust to
  whatever the actual call-to-action should be at post time (e.g. a specific docs link, a NuGet
  package name once one exists to point to).
