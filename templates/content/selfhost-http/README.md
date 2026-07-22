# Benzene Self-Hosted HTTP starter

A Benzene HTTP service that runs in a plain .NET generic host — a `System.Net.HttpListener`-based
server owned by Benzene, with no ASP.NET/Kestrel. The self-hosted counterpart of the `benzene.asp`
template: the same handlers, no web framework, one small console app.

## Run it

```bash
dotnet run
# in another shell:
curl http://localhost:8080/hello/world     # → {"message":"Hello world!"}
```

The listen URL and concurrency are set in `StartUp.cs` (`BenzeneHttpConfig`).

## What's here

- `HelloWorldMessageHandler.cs` — your business logic. A request/response handler mapped to a topic
  (`[Message("hello:world")]`) and an HTTP route (`[HttpEndpoint("GET", "/hello/{name}")]`). It knows
  nothing about the transport — the same handler runs unchanged behind ASP.NET, AWS Lambda, Azure
  Functions, or a queue worker (that's the point).
- `StartUp.cs` — the transport wiring: `app.UseWorker(worker => worker.UseHttp(...))`. This is the one
  part that changes if you move the handler to another host.
- `Program.cs` — the generic host that runs the listener as a background service.

Add more handlers alongside `HelloWorldMessageHandler` — they're discovered automatically by
reflection, no routing table to maintain.

See [docs/getting-started.md](https://github.com/daniellepelley/benzene) and `docs/self-hosting.md`.
