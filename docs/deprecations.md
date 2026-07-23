# Deprecations

Packages and APIs that are still shipped and functional but are **no longer recommended** and may be
removed in a future major version. Each entry says what to use instead. Deprecated code carries an
`[Obsolete]` attribute (a compiler warning, never an error) so a build surfaces the nudge at the call
site.

> **Guiding principle.** A Benzene adapter has to earn its place. A self-hosted transport that merely
> wraps a slower standard-library server — adding no performance or capability advantage over calling
> that library directly — doesn't. When Benzene already offers a faster path for the same job, the
> weaker one is deprecated rather than kept for parity's sake.

## `Benzene.SelfHost.Http` — deprecated

**Use instead:** [`Benzene.AspNet.Core`](hosting.md) (Kestrel).

`Benzene.SelfHost.Http` hosts HTTP on `System.Net.HttpListener`. On .NET's cross-platform managed
`HttpListener` that is **materially slower than Kestrel** (Kestrel is the server the whole ASP.NET
Core ecosystem is tuned around), and wrapping it in Benzene adds no throughput or feature advantage
over the raw listener — so it fails the guiding principle above. Benzene already has a
production-grade HTTP host, `Benzene.AspNet.Core`, running on Kestrel, so there is no reason to reach
for the `HttpListener` one.

- **What's deprecated:** the `UseHttp(...)` worker extension (marked `[Obsolete]`). `BenzeneHttpWorker`,
  `BenzeneHttpConfig`, `SelfHostHttpContext`, and the health-check extensions still function so existing
  apps keep building.
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
- **Worker that also needs an HTTP health/probe endpoint** (e.g. a Kafka/SQS consumer exposing
  `GET /livez` for Kubernetes) — the one case where `HttpListener`'s throughput genuinely doesn't
  matter. Even so, host it on Kestrel: run a `WebApplication` that registers your worker as an
  `IHostedService` (`Host…UseBenzene<StartUp>()` glue) *and* serves the health checks via
  `Benzene.AspNet.Core` in the same process. One host, one HTTP server, no second HTTP stack.
- **Why not just make it faster?** A self-host that beats `HttpListener` without pulling in ASP.NET
  Core would mean hand-rolling an HTTP/1.1 server on raw sockets — a large, hard-to-harden effort that
  would, at best, reinvent Kestrel. Kestrel already exists and is battle-tested, so the pragmatic
  answer is to point at it.
