# Benzene.Example.Cloudflare

> **⚠️ Experimental / community-supported — not part of the Benzene 1.0 support commitment.**
> Cloudflare is **out of scope for the 1.0 release**; it works but receives less testing and no
> API-stability guarantee than the AWS / Azure / ASP.NET / self-hosted surfaces.

A minimal ASP.NET Core Benzene app, deployed behind [Cloudflare Containers](https://developers.cloudflare.com/containers/)
via a thin Worker that proxies HTTP traffic into a Docker container running this project.

This is a deployment pattern, not a new Benzene package: `Benzene.AspNet.Core` already runs
unchanged inside any container, the same way it already runs on IIS/AKS/Elastic Beanstalk.

See [docs/getting-started-cloudflare.md](../../docs/getting-started-cloudflare.md) for the full
walkthrough (project setup, the Dockerfile, and the Cloudflare-side `wrangler.toml`/Worker config
in [`worker/`](worker)).

**Not independently deployed or verified against a live Cloudflare account** — the Dockerfile and
`worker/` config were hand-checked against current Cloudflare Containers docs, but not run through
`wrangler deploy`. Review before using in production, the same caveat
[`examples/Aws/Benzene.Examples.Aws/template.yaml`](../Aws/Benzene.Examples.Aws/template.yaml) carries
for AWS SAM.
