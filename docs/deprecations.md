# Deprecations & removals

Packages and APIs that are **no longer recommended** — either still shipped but deprecated (carrying an
`[Obsolete]` compiler warning, never an error), or removed from the repo entirely. Each entry says
what to use instead.

> **Guiding principle.** A Benzene adapter has to earn its place. A self-hosted transport that merely
> wraps a slower standard-library server — adding no performance or capability advantage over calling
> that library directly — doesn't. When Benzene already offers a faster path for the same job, the
> weaker one is dropped rather than kept for parity's sake.

## `Benzene.SelfHost.Http` — removed

**Use instead:** [`Benzene.AspNet.Core`](hosting.md) (Kestrel).

`Benzene.SelfHost.Http` hosted HTTP on `System.Net.HttpListener`. On .NET's cross-platform managed
`HttpListener` that is **materially slower than Kestrel** (Kestrel is the server the whole ASP.NET
Core ecosystem is tuned around), and wrapping it in Benzene added no throughput or feature advantage
over the raw listener — so it failed the guiding principle above. Benzene already has a
production-grade HTTP host, `Benzene.AspNet.Core`, running on Kestrel, so there was no reason to keep
the `HttpListener` one. It was deprecated (`UseHttp(...)` marked `[Obsolete]`), then removed from the
repo; the guidance below is for anyone still on an older published version.
- **Migration:** a `BenzeneStartUp`'s `Configure` is platform-neutral — the same handlers, middleware,
  and health checks move across unchanged. Swap the host wiring:

  ```csharp
  // Before — Benzene.SelfHost.Http (HttpListener)
  IHost host = Host.CreateDefaultBuilder(args)
      .UseBenzene<StartUp>()   // StartUp.Configure calls app.UseWorker(w => w.UseHttp(...))
      .Build();

  // After — Benzene.AspNet.Core (Kestrel)
  var builder = WebApplication.CreateBuilder(args).UseBenzene<StartUp>();
  var app = builder.Build();
  app.UseBenzene();            // StartUp.Configure calls app.UseHttp(http => http.UseMessageHandlers())
  app.Run();
  ```

  See [`docs/hosting.md`](hosting.md) for both host shapes and the `benzene.asp` starter template
  (`templates/content/asp`) for a complete Kestrel-hosted example.
- **Want it *lean* — Kestrel without the MVC/routing baggage?** That's usually why people reached for
  `SelfHost.Http` in the first place. Use `WebApplication.CreateSlimBuilder(args)` (or, for an even
  smaller surface, `CreateEmptyBuilder(...)`) instead of `CreateBuilder`. The slim builder starts from
  just Kestrel + configuration + logging and opts you into nothing else — it's the trim/AOT-friendly
  host Microsoft ships for exactly this "no framework baggage" case, and Benzene layers on top of it
  unchanged:

  ```csharp
  // Lean Kestrel, no MVC/routing - the closest replacement for the "no ASP.NET" motivation
  var builder = WebApplication.CreateSlimBuilder(args).UseBenzene<StartUp>();
  var app = builder.Build();
  app.UseBenzene();
  app.Run();
  ```

  This still references the `Microsoft.AspNetCore.App` shared framework (Kestrel only ships there),
  so it isn't dependency-free the way `HttpListener` was — but it *is* the leanest Kestrel host, and a
  bespoke Kestrel-on-raw-`IHttpApplication` server would save only the middleware-pipeline overhead
  (which Benzene bypasses anyway) at the cost of owning request/response feature mapping, TLS, HTTP/2,
  connection limits, and graceful drain yourself.
- **Worker that also needs an HTTP health/probe endpoint** (e.g. a Kafka/SQS consumer exposing
  `GET /livez` for Kubernetes) — the one case where `HttpListener`'s throughput genuinely doesn't
  matter. Even so, host it on Kestrel: run a `WebApplication` that registers your worker as an
  `IHostedService` (`Host…UseBenzene<StartUp>()` glue) *and* serves the health checks via
  `Benzene.AspNet.Core` in the same process. One host, one HTTP server, no second HTTP stack.
- **Why not just make it faster?** A self-host that beats `HttpListener` without pulling in ASP.NET
  Core would mean hand-rolling an HTTP/1.1 server on raw sockets — a large, hard-to-harden effort that
  would, at best, reinvent Kestrel. Kestrel already exists and is battle-tested, so the pragmatic
  answer is to point at it.
