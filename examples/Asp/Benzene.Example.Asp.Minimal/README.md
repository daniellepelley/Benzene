# Benzene ASP.NET Core Example (Minimal)

The smallest thing that works: the runnable version of the
[Getting Started](../../../docs/getting-started.md) walkthrough. A single endpoint,
`GET /hello/{name}`, that returns a JSON greeting - nothing else. Start here if you're meeting
Benzene for the first time.

For a fuller ASP.NET Core host (Spec UI, validation, an OAuth2-protected route), see the sibling
[`Benzene.Example.Asp`](../Benzene.Example.Asp) project.

## The three moving parts

- **`HelloWorldMessageHandler.cs`** - your logic. It receives a typed request, returns a typed
  response, and knows nothing about HTTP. This is the only file you'd carry over unchanged to AWS
  Lambda or Azure Functions.
- **`StartUp.cs`** - a platform-neutral `BenzeneStartUp`: registers the handler
  (`ConfigureServices`) and wires it onto HTTP (`Configure` -> `UseHttp` -> `UseMessageHandlers`).
- **`Program.cs`** - the ASP.NET Core host: `builder.UseBenzene<StartUp>()` then `app.UseBenzene()`.

This is the same `BenzeneStartUp` model every other host example uses (see `examples/CLAUDE.md`) -
only the transport wired inside `Configure` changes between hosts.

## Running

```bash
dotnet run --project Benzene.Example.Asp.Minimal --urls http://localhost:5000
```

Then, in another terminal:

```bash
curl http://localhost:5000/hello/world
# => {"message":"Hello world!"}
```

Benzene mapped `GET /hello/{name}` to the `hello:world` topic, bound `world` onto
`HelloWorldRequest.Name`, invoked the handler, and serialised the result back as JSON.
