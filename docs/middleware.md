# Middleware

Middleware in Benzene is the pipeline mechanism that every request/event flows through on its way
to (and back from) a handler. Each middleware component sits in a chain of responsibility: it can
act before calling the next middleware, act after it returns, or stop the chain entirely by not
calling `next()`. This is the core building block hexagonal "ports" are wired up with — HTTP, AWS
Lambda (API Gateway, SQS, SNS, Kafka), Azure Functions, and ASP.NET Core all configure the same
kind of pipeline, just with a different `TContext`.

## Core abstractions

These live in `Benzene.Abstractions.Middleware`.

### `IMiddleware<TContext>`

```csharp
public interface IMiddleware<in TContext>
{
    string Name { get; }
    Task HandleAsync(TContext context, Func<Task> next);
}
```

`TContext` is contravariant (`in`), which is what lets a single piece of middleware written against
a base/shared context type be reused across pipelines whose context is a more derived type.
`Name` identifies the middleware for logging/diagnostics (see [Automatic `Activity` wrapping](#automatic-activity-wrapping-imiddlewarewrapper) below).

Implementations decide when to call `next`:

```csharp
public class MyMiddleware : IMiddleware<MyContext>
{
    public string Name => "MyMiddleware";

    public async Task HandleAsync(MyContext context, Func<Task> next)
    {
        // runs on the way in
        await next(); // continue the chain — omit this to short-circuit
        // runs on the way back out
    }
}
```

### `IMiddlewarePipeline<TContext>`

The built, executable pipeline:

```csharp
public interface IMiddlewarePipeline<TContext>
{
    Task HandleAsync(TContext context, IServiceResolver serviceResolver);
}
```

Pipelines are immutable and reusable/thread-safe once built — the same `IMiddlewarePipeline<T>`
instance can process many requests concurrently.

### `IMiddlewarePipelineBuilder<TContext>`

The fluent builder used to compose a pipeline:

```csharp
public interface IMiddlewarePipelineBuilder<TContext> : IRegisterDependency
{
    IMiddlewarePipelineBuilder<TContext> Use(Func<IServiceResolver, IMiddleware<TContext>> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IMiddlewarePipeline<TContext> Build();
}
```

- `Use` registers a middleware factory; middleware runs in registration order (first registered
  runs first on the way in, last on the way out).
- `Create<TNewContext>()` creates a **new** builder for a different context type that shares the
  same underlying dependency registration (`IRegisterDependency`) — this is what backs `.Split()`
  and `.Convert()` (see below).
- `Build()` finalizes the pipeline; nothing can be added to it afterwards.

## Concrete implementations (`Benzene.Core.Middleware`)

- **`MiddlewarePipeline<TContext>`** — the `IMiddlewarePipeline<TContext>` implementation. It
  reverse-aggregates the registered middleware factories into a single `Func<Task>` chain (each
  middleware closes over the "rest of the pipeline" as its `next`), and caches that chain after
  first use. Every middleware instance is resolved through the current `IMiddlewareFactory` (see
  below) before being invoked — this is what lets wrappers apply to *every* middleware, not just
  ones you explicitly configure.
- **`MiddlewarePipelineBuilder<TContext>`** — the `IMiddlewarePipelineBuilder<TContext>`
  implementation. Its constructor calls `registerDependency.Register(x => x.AddBenzeneMiddleware())`,
  so `IMiddlewareFactory` and the DI service resolver are always registered as soon as you create a
  builder.
- **`DefaultMiddlewareFactory`** — the default `IMiddlewareFactory`. It takes every
  `IMiddlewareWrapper` registered in DI (`IEnumerable<IMiddlewareWrapper>`) and aggregates them
  around each middleware instance as it's created:

  ```csharp
  public IMiddleware<TContext> Create<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
  {
      return middlewareWrappers.Aggregate(middleware, (m, wrapper) => wrapper.Wrap(serviceResolver, m));
  }
  ```

  If no `IMiddlewareFactory` is registered at all, `MiddlewarePipeline<TContext>` falls back to a
  `DefaultMiddlewareFactory` with an empty wrapper list (i.e. middleware runs unwrapped).

## Building a pipeline

A typical AWS Lambda `StartUp.Configure` composes several pipelines, one per transport:

```csharp
public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
{
    var benzeneMessagePipeline = app.Create<BenzeneMessageContext>()
        .UseTimer("benzene-message-application")
        .UseHealthCheck("healthcheck", healthChecks)
        .UseMessageHandlers(router => router
            .UseFluentValidation());

    app.UseBenzeneMessage(benzeneMessagePipeline);

    app.UseSqs(sqsApp => sqsApp
        .UseTimer("sqs-application")
        .UseMessageHandlers(router => router.UseFluentValidation()));
}
```

## The `.Use(...)` family

`Benzene.Core.Middleware.Extensions` provides several `.Use(...)` overloads so you rarely need to
write a dedicated `IMiddleware<TContext>` class:

```csharp
// 1. An existing middleware instance
app.Use(new MyMiddleware());

// 2. Inline, unnamed function middleware
app.Use(async (context, next) => { await next(); });

// 3. Inline, named function middleware (name shows up in diagnostics)
app.Use("my-middleware", async (context, next) => { await next(); });

// 4. Inline middleware with access to IServiceResolver (resolved once, when the func is registered)
app.Use(resolver => new Func<MyContext, Func<Task>, Task>(async (context, next) =>
{
    var logger = resolver.GetService<ILoggerFactory>().CreateLogger("my-middleware");
    await next();
}));

// 5. Same as 4, but named
app.Use("my-middleware", resolver => new Func<MyContext, Func<Task>, Task>(async (context, next) => { await next(); }));

// 6. Inline middleware with IServiceResolver resolved per-request (not once at registration time)
app.Use(async (resolver, context, next) =>
{
    var logger = resolver.GetService<ILoggerFactory>().CreateLogger("my-middleware");
    logger.LogInformation("handling");
    await next();
});

// 7. Same as 6, but named
app.Use("my-middleware", async (resolver, context, next) => { await next(); });

// 8. Resolve a middleware type from DI
app.Use<MyContext, MyMiddleware>();
```

Overloads 4/5 receive the resolver once when the `Func` factory itself runs (at pipeline-build
time); overloads 6/7 receive it on every invocation of the middleware (per-request), which matters
if you need request-scoped services. All of these end up constructing a
**`FuncWrapperMiddleware<TContext>`** under the hood — the class that lets a plain
`Func<TContext, Func<Task>, Task>` satisfy `IMiddleware<TContext>`:

```csharp
public class FuncWrapperMiddleware<TContext>(string name, Func<TContext, Func<Task>, Task> func) : IMiddleware<TContext>
{
    public string Name { get; } = !string.IsNullOrEmpty(name) ? name : Constants.Unnamed;
    public Task HandleAsync(TContext context, Func<Task> next) => func(context, next);
}
```

Middleware added without a name gets `Constants.Unnamed` ("Unnamed") as its `Name`.

## `.OnRequest()` / `.OnResponse()`

Tap points for code that only needs to run before or after the rest of the pipeline, without
manually writing the `await next()` dance:

```csharp
app.OnRequest("request-demo", (resolver, context) =>
{
    var logger = resolver.GetService<ILoggerFactory>().CreateLogger("request-demo");
    logger.LogInformation("incoming");
});

app.OnResponse("response-demo", context =>
{
    // runs after everything downstream has completed
});
```

Both have four overloads each: with/without a `name`, and with/without an `IServiceResolver`
parameter on the action. `OnRequest` calls your action then `next()`; `OnResponse` calls `next()`
then your action — internally both are implemented via `.Use(...)`/`FuncWrapperMiddleware`.

## `.Split()`

Branches the pipeline conditionally. If the predicate matches, the branch pipeline runs *instead
of* the rest of the outer pipeline (the outer `next()` is not called); otherwise the outer pipeline
continues as normal:

```csharp
app.Split(context => context.Topic == "special-case", branch => branch
    .Use("special-handling", async (context, next) => { /* ... */ await next(); }));
```

`Split` has two overloads: a plain `Func<TContext, bool>` predicate, or an `IContextPredicate<TContext>`
(`bool Check(TContext context, IServiceResolver serviceResolver)`) when the routing decision needs
DI-resolved services. Under the hood, `Split` calls `app.Create<TContext>()` to build the branch
pipeline (sharing dependency registration with the parent), builds it, and wraps the check in a
`FuncWrapperMiddleware` named `"Split"`.

## `.Convert()` / `ContextConverterMiddleware`

Converts to a different context type for an inner pipeline, then maps the inner pipeline's result
back onto the outer context — this is how, for example, `UseBenzeneMessage(...)` bridges a
transport-specific context (`AwsEventStreamContext`, `SqsMessageContext`, ...) into the shared
`BenzeneMessageContext` pipeline. The conversion contract is `IContextConverter<TContextIn, TContextOut>`:

```csharp
public interface IContextConverter<TContextIn, TContextOut>
{
    Task<TContextOut> CreateRequestAsync(TContextIn contextIn);
    Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut);
}
```

Four `.Convert(...)` overloads exist, letting you supply either a converter instance or two
inline `Func`/`Action` delegates (backed by `InlineContextConverter<TIn, TOut>`), and either an
already-built `IMiddlewarePipeline<TContextOut>` or an `Action<IMiddlewarePipelineBuilder<TContextOut>>`
to configure one inline:

```csharp
app.Convert(
    createContextFunc: outer => new InnerContext(outer),
    mapContext: (outer, inner) => outer.Result = inner.Result,
    action: inner => inner.UseMessageHandlers());
```

All four overloads ultimately add a **`ContextConverterMiddleware<TContext, TContextOut>`**, whose
`HandleAsync` does not call the outer `next` at all — it converts, runs the inner pipeline, then
maps the response back:

```csharp
public async Task HandleAsync(TContext context, Func<Task> next)
{
    var contextOut = await converter.CreateRequestAsync(context);
    await middlewarePipeline.HandleAsync(contextOut, serviceResolver);
    await converter.MapResponseAsync(context, contextOut);
}
```

## `.UseExceptionHandler()`

Adds a centralized try/catch around everything after it in the pipeline:

```csharp
app.UseExceptionHandler((context, exception) =>
{
    // set an error result on context, e.g. context.Result = BenzeneResult.UnexpectedError();
});
```

This registers an `ExceptionHandlerMiddleware<TContext>`, which logs the exception at `Error` level
(via `ILoggerFactory`, falling back to a `NullLogger` if none is registered) and then invokes your
`onException` callback with the context and exception — it does not rethrow.

## `MiddlewareRouter<TRequest, TContext>`

An abstract base class for building routing middleware that dispatches to different handling logic
based on something extracted from the request/context (`Benzene.Core.MessageHandlers`'s
`MessageRouter<TContext>` is itself just an `IMiddleware<TContext>`, not built on this class — this
base class is for writing your *own* request-shaped routers). You implement three hooks:

```csharp
public abstract class MiddlewareRouter<TRequest, TContext>(IServiceResolver serviceResolver) : IMiddleware<TContext>
{
    protected abstract TRequest TryExtractRequest(TContext context);
    protected abstract bool CanHandle(TRequest request);
    protected abstract Task HandleFunction(TRequest request, TContext context, IServiceResolverFactory serviceResolverFactory);
}
```

If `TryExtractRequest` returns `null`, or `CanHandle` returns `false`, the router calls `next()` so
another piece of middleware (or another router) gets a chance; otherwise it calls `HandleFunction`
and does not call `next()`.

## Automatic `Activity` wrapping (`IMiddlewareWrapper`)

`IMiddlewareWrapper` (`Benzene.Abstractions.Middleware`) is the decorator contract every middleware
instance in every pipeline passes through on its way out of `DefaultMiddlewareFactory`:

```csharp
public interface IMiddlewareWrapper
{
    IMiddleware<TContext> Wrap<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware);
}
```

Any `IMiddlewareWrapper` registered in DI (as `IMiddlewareWrapper`, not just its concrete type) is
picked up automatically — you never call `.Wrap(...)` yourself. `AddBenzeneMiddleware()` (called
for you by `MiddlewarePipelineBuilder<TContext>`'s constructor) registers `DefaultMiddlewareFactory`
as `IMiddlewareFactory`; that factory pulls in whatever `IMiddlewareWrapper`s are registered and
decorates every middleware instance with all of them, in registration order, as the pipeline
executes.

`Benzene.Diagnostics`'s `AddDiagnostics()` is the real, working example of this mechanism: it
registers **`ActivityMiddlewareWrapper`**, which wraps every middleware in an
**`ActivityMiddlewareDecorator<TContext>`**:

```csharp
public class ActivityMiddlewareWrapper : IMiddlewareWrapper
{
    public IMiddleware<TContext> Wrap<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return new ActivityMiddlewareDecorator<TContext>(middleware, serviceResolver);
    }
}
```

`ActivityMiddlewareDecorator<TContext>` starts a `System.Diagnostics.Activity` named after the
inner middleware's `Name` before calling `HandleAsync`, and tags it with whatever it can resolve
from the current context:

```csharp
public async Task HandleAsync(TContext context, Func<Task> next)
{
    using var activity = BenzeneDiagnostics.ActivitySource.StartActivity(Name);
    if (activity is not null)
    {
        Tag(activity, context);
    }

    await _inner.HandleAsync(context, next);
}
```

Tags applied when resolvable: `benzene.transport` (from `ICurrentTransport`), `benzene.topic` /
`benzene.version` (from `IMessageGetter<TContext>`), and `benzene.handler` (from
`IMessageHandlerDefinitionLookUp`, looked up by topic). Because this happens inside
`DefaultMiddlewareFactory.Create`, it applies uniformly to hand-written middleware, `.Use(...)`
inline middleware, `MessageRouter<TContext>`, and everything else in the pipeline — with no
per-middleware opt-in. `Benzene.Diagnostics` also ships a second `IMiddlewareWrapper`
(`DebugMiddlewareWrapper`/`DebugMiddlewareDecorator<TContext>`) that writes `Debug.WriteLine`
start/stop lines instead — unrelated to tracing, purely a dev-time aid — both wrappers are added
together by `AddDiagnostics()` and both apply to every middleware simultaneously since
`DefaultMiddlewareFactory` aggregates *all* registered wrappers.

Enable it once, at startup:

```csharp
services.UsingBenzene(x => x.AddDiagnostics());
```

See [Monitoring & Diagnostics](monitoring#tracing) for how these spans get exported to a real
tracing backend via `Benzene.OpenTelemetry`, and [Common Middleware](common-middleware#usetimer) for
`UseTimer`, which opens an additional, explicitly-named `Activity` around a whole pipeline stage
(distinct from the automatic per-middleware spans described here).

## See also

- [Common Middleware](common-middleware) — the ready-made middleware Benzene ships (correlation
  IDs, timers, metrics, health checks, message handler routing, validation).
- [Monitoring & Diagnostics](monitoring) — tracing, metrics, logging, and distributed trace
  propagation built on top of the mechanisms described here.
- [Message Handlers](message-handlers) — how `MessageRouter<TContext>` and `UseMessageHandlers(...)`
  use this pipeline to dispatch to individual handlers.
