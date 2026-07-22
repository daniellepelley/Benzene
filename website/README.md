# Benzene website

The source for [benzene](https://github.com/daniellepelley/Benzene)'s marketing + documentation
site: a small .NET console app (`generator/`) that produces a plain static HTML site — no server
runtime, no Node/Ruby toolchain, deployable by copying the output to any static file host (an
`aws s3 sync` in this repo's case).

## How it works

The generator (`website/generator`, see its own `CLAUDE.md`) builds two kinds of pages:

- **The marketing home page** (`index.html`) is hand-authored from `MarketingContent.cs` (hero
  tagline, feature cards, quickstart snippets, platform list) and `ArchitectureDiagram.cs` (the
  hexagon-with-six-adapters SVG diagram) — a card/hero layout isn't something plain markdown can
  express well, so this isn't derived from README.md.
- **Docs pages** are crawled from `docs/index.md` (whose nested link list also becomes the site's
  sidebar navigation) and converted from Markdown to HTML with
  [Markdig](https://github.com/xoofx/markdig).

Both page types share a layout (`Layout.cs`) styled by `generator/assets/site.css`, and the same
hexagon-with-inscribed-ring logo (`Logo.cs`) as favicon, header icon, and hero graphic. The
project isn't part of `Benzene.sln` — same as `templates/` and `benchmarks/`, it's its own
standalone project.

## Building locally

From the repo root:

```bash
dotnet run --project website/generator -- --out website/dist
```

This writes the full static site to `website/dist/` (gitignored). To preview it:

```bash
cd website/dist && python3 -m http.server 8080
```

then open <http://localhost:8080>.

The generator fails (non-zero exit) if any internal link it generated doesn't resolve to an
actual output file — a cheap built-in broken-link check, so a bad rewrite can't ship silently.

## Deployment

The site is hosted on AWS at **[benzene.app](https://benzene.app)** with a dev/live split:

- **`.github/workflows/deploy-website.yml`** — on every push to `main` touching `docs/**`,
  `README.md`, or `website/**` (plus manual `workflow_dispatch`), it runs the generator and syncs
  `website/dist/` to the **dev** bucket, then invalidates the dev CloudFront distribution →
  **[dev.benzene.app](https://dev.benzene.app)**.
- **`.github/workflows/promote-website.yml`** — a manual promotion that copies the dev bucket to the
  **live** bucket (an S3 `dev → live` copy, no rebuild) and invalidates the production distribution →
  **[benzene.app](https://benzene.app)**. So what you review on dev is exactly what goes live.

Both reuse the same static-credential pattern as `deploy-aws-example.yml`
(`vars.AWS_ACCESS_KEY_ID` / `secrets.AWS_SECRET_ACCESS_KEY`, `vars.WEBSITE_AWS_REGION`) plus the
bucket/distribution variables listed in [`deploy/README.md`](deploy/README.md).

The AWS infrastructure — two private S3 buckets, CloudFront distributions (HTTPS is mandatory: `.app`
is HSTS-preloaded, so plain S3 website endpoints won't do), an ACM certificate, the Route 53 zone, and
the `www → apex` redirect — is Terraform under **[`deploy/`](deploy/)**. See
[`deploy/README.md`](deploy/README.md) for the one-time setup (apply, GoDaddy nameserver switch, repo
variables, CI IAM policy).
