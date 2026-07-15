# Benzene Google Cloud Example

Demonstrates Benzene's Google Cloud integration (Phase 0 of
[`work/google-cloud-roadmap-1.0.md`](../../work/google-cloud-roadmap-1.0.md)): **one**
platform-neutral `Startup : BenzeneStartUp` class, hosted unchanged on both of Google Cloud's
HTTP-serving compute targets.

## What this shows

- `Benzene.Examples.Google/Startup.cs` - the single application definition. It wires the shared
  `Benzene.Examples.App` order handlers onto HTTP (`app.UseHttp(asp => asp.UseMessageHandlers(...))`)
  and never references Cloud Run or Cloud Functions directly.
- `Program.cs` - hosts `Startup` on **Cloud Run** (the recommended target - a plain ASP.NET Core
  container, no Functions Framework dependency) via `Benzene.AspNet.Core`'s existing
  `WebApplicationBuilder.UseBenzene<Startup>()` / `app.UseBenzene()`, binding Kestrel to the `PORT`
  env var Cloud Run injects.
- `Function.cs` - hosts the **exact same** `Startup` on **Cloud Functions Gen2** instead, via
  `Benzene.GoogleCloud.Functions.Http.GoogleCloudFunctionHost<Startup>`. Nothing in `Startup.cs`
  changes between the two - see that package's `CLAUDE.md` for why (`IAspApplicationBuilder` has no
  inherent dependency on a live ASP.NET Core `IApplicationBuilder`).
- `Benzene.Examples.Google.Tests` - exercises the app through
  `Benzene.GoogleCloud.Functions.Http.TestHelpers.BuildGoogleCloudFunctionHost<Startup>()` +
  `SendHttpAsync(...)`, dispatching real `HttpContext`s through the full pipeline without a live
  Kestrel server or Functions Framework host.

## Run it

### Cloud Run (recommended)

Locally:

```bash
cd examples/Google/Benzene.Examples.Google
dotnet run
```

The app listens on `PORT` (default `8080` if unset) - try `curl http://localhost:8080/orders`.

Deploy (build context is the repo root - see the `Dockerfile`'s own comment for why):

```bash
docker build -f examples/Google/Benzene.Examples.Google/Dockerfile -t benzene-google-example .
gcloud run deploy benzene-google-example --image benzene-google-example --port 8080
```

### Cloud Functions Gen2

```bash
cd examples/Google/Benzene.Examples.Google
gcloud functions deploy benzene-google-example \
  --gen2 \
  --runtime=dotnet10 \
  --entry-point=Benzene.Examples.Google.Function \
  --trigger-http \
  --allow-unauthenticated
```

## What to look for

- `Startup.cs` is the only file either deploy target's request handling touches - `Program.cs` and
  `Function.cs` are each under 10 lines of host-specific glue.
- `Benzene.GoogleCloud.Functions.Http/GoogleCloudFunctionApplicationBuilder.cs`'s remarks, for the
  mechanism that makes this possible.

## Notes

- Live GCP deployment was not exercised in this environment (no live GCP project / outbound access
  to Google Cloud APIs in this sandbox) - the `dotnet test` suite and a direct in-process
  `HandleAsync` round-trip through `Function` are what's actually been verified. Treat the `gcloud`
  commands above as documented, not verified, until you've run them against a real project.
