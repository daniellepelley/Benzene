## gRPC

Benzene's gRPC integration is more complete than a first glance suggests: the server side handles all four RPC shapes, maps status codes, propagates the deadline into the cancellation token, bridges metadata both ways, and offers a real per-service-name health split. The gaps are concentrated on the **client** side and the **richer edges** of the gRPC contract.

---

**[DIVERGENCE] Client discards the caller's deadline and cancellation token** (Severity: High)
- **Benzene today:** `GrpcContextConverter<T>.CreateRequestAsync` hard-codes `deadline: null, CancellationToken.None` (`GrpcContextConverter.cs:25`), and `GrpcBenzeneMessageClient.SendMessageAsync` builds its outbound context through exactly that converter. `GrpcSendMessageContext` *has* `Deadline`/`CancellationToken` fields and `GrpcClientRoute` faithfully puts them into `CallOptions` — but nothing ever populates them with a non-null/non-`None` value. `IBenzeneMessageClient.SendMessageAsync` also takes no `CancellationToken`.
- **gRPC intent:** Deadline and cancellation propagation across a service boundary is a first-class .NET gRPC feature. A server handler that fans out downstream should pass its *remaining* deadline and inbound cancellation token onward so the whole call tree fails fast and stops orphaned work.
- **Impact:** A Benzene server observing its own deadline (which it does) still issues downstream calls with **no deadline and no cancellation**. Client cancel / deadline-exceeded upstream does not abort the outbound RPC; downstream work runs to completion. The single most consequential gap.
- **Recommendation:** Thread the ambient `ICancellationTokenAccessor` token and a computed deadline (from `ServerCallContext.Deadline`) into `GrpcSendMessageContext`; consider an overload of `SendMessageAsync` accepting a `CancellationToken`.

**[MISSING] Client supports unary only — no streaming of any kind** (Severity: Medium-High)
- `GrpcClientRouteRegistry.Add` hard-codes `MethodType.Unary` and `GrpcClientRoute.InvokeAsync` calls only `invoker.AsyncUnaryCall`. No client-streaming, server-streaming, or duplex call site. Asymmetry — a Benzene service can *host* a server-streaming/duplex method but cannot *call* one through `IBenzeneMessageClient`. Either add streaming client routes (mirroring the server's `GrpcStreamAdapter`) or explicitly document the client as unary-only.

**[MISSING] No rich error model (google.rpc.Status / error details)** (Severity: Medium)
- Non-OK results become `new RpcException(new Status(statusCode, detail))` where `detail` is the Benzene errors joined with `"; "`; the only structured signal is the custom `benzene-status` trailer. The standard rich-error model conveys structured details via `google.rpc.Status` in the `grpc-status-details-bin` trailer (`ErrorInfo`/`BadRequest.FieldViolation` etc.). Validation failures collapse to a flat string; clients can't machine-read field-level errors. Optionally emit `google.rpc.Status` details (esp. `ValidationError` → `BadRequest.FieldViolations`).

**[DIVERGENCE] Mid-stream and non-mapped failures fall back to UNKNOWN/INTERNAL** (Severity: Low-Medium)
- For server-streaming/duplex, status is mapped and thrown *before* items are written, then `GrpcStreamAdapter.WriteAll` enumerates the handler stream. An exception thrown *during* enumeration is not routed through `IGrpcStatusCodeMapper` → surfaces as gRPC `UNKNOWN`. Unmapped Benzene statuses default to `INTERNAL`. Wrap stream enumeration so faults map through the same status mapper, or document the boundary.

**[WRONG-APPROACH] Benzene interceptor substitutes the continuation, bypassing later interceptors** (Severity: Low)
- On a matched route the interceptor calls `base.UnaryServerHandler(request, context, handler.HandleAsync)`, replacing the real continuation with the Benzene handler. gRPC composes interceptors so the `continuation` *is* the remainder of the chain plus the service method. Any interceptor registered *after* `BenzeneInterceptor` never runs for Benzene-routed methods. Document that Benzene-owned methods short-circuit the chain; advise placing cross-cutting interceptors *before* `BenzeneInterceptor` or as Benzene middleware.

**[MISSING] Inbound binary metadata is silently dropped** (Severity: Low)
- `GrpcMessageHeadersGetter` skips every `entry.IsBinary` header; `-bin` metadata keys are a normal part of the metadata contract. Middleware relying on binary headers (e.g. propagated binary trace context) can't see them. Honest limitation given Benzene's `string`-keyed header abstraction; worth a doc line.

---

**Correctly handled (not findings):** server-side all-four-shape dispatch is genuinely complete; status-code mapping is bidirectional and sensible with the `benzene-status` trailer recovering statuses that collapse to `OK` on the wire; the server observes the deadline and distinguishes `DeadlineExceeded` vs `Cancelled` and seeds the ambient token; response headers/trailers map both directions; health checking rides the standard `Grpc.AspNetCore.HealthChecks` (so `Check` **and** `Watch` per-service-name work) with a real liveness/readiness split; reflection is opt-in. Channel/credentials/keepalive/message-size/compression/LB/retry correctly left to the app-owned `GrpcChannel`. Doc nit: reflection described as `v1alpha` only while `MapGrpcReflectionService` registers current reflection.

**Verdict:** Server-side gRPC is solid and idiomatic; the real weaknesses are all client-side — broken deadline/cancellation propagation (High) and a unary-only client — plus the absence of gRPC's rich error model.
