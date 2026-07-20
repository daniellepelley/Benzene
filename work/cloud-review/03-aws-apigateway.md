## AWS API Gateway

Scope: every `.cs` and `CLAUDE.md` in `src/Benzene.Aws.Lambda.ApiGateway` (incl. `ApiGatewayCustomAuthorizer/`) plus shared entrypoint/serialization in `src/Benzene.Aws.Lambda.Core`. Verified against API Gateway proxy integration, payload format versions 1.0 vs 2.0, binary media types, and Lambda authorizers.

---

**[DIVERGENCE] Payload format version 2.0 (HTTP API) is entirely unsupported, and a v2 event silently fails the invocation** (Severity: High)
- **Benzene today:** Only `APIGatewayProxyRequest`/`APIGatewayProxyResponse` (v1.0) are wrapped. `ApiGatewayLambdaHandler.CanHandle` claims an invocation only when `request?.HttpMethod != null` (`ApiGatewayLambdaHandler.cs:42`). In a v2.0 event the method lives at `requestContext.http.method`, and top-level `httpMethod` doesn't exist — so `HttpMethod` is null, `CanHandle` returns false, no middleware handles the event, and `AwsLambdaEntryPoint.FunctionHandlerAsync` throws `BenzeneException("The event type has not been recognized…")` (`AwsLambdaEntryPoint.cs:53`). CLAUDE.md documents this as deliberate ("Payload format version 1.0 only").
- **AWS intent:** When you attach a Lambda proxy integration to an **HTTP API**, the default `payloadFormatVersion` is **2.0** — a different wire shape (`version`/`routeKey`/`rawPath`, comma-joined `headers`, `cookies[]`, `requestContext.http.{method,path}`, and a response that may be a bare JSON value).
- **Impact:** A developer who creates an HTTP API with defaults gets a hard runtime failure with a misleading "event not recognized" message, not a graceful 4xx. The two most common front doors (REST API v1, HTTP API v2-by-default) are not both covered. Biggest gap.
- **Recommendation:** Add a parallel v2 adapter set (`APIGatewayHttpApiV2ProxyRequest`/`Response`) with its own `CanHandle` and a `UseApiGatewayV2` registration, or at minimum detect a v2 shape and surface an explicit "payload format 2.0 not supported — set payloadFormatVersion 1.0" error.

**[WRONG-APPROACH] Binary bodies are forced through a UTF-8 string round-trip, corrupting true binary payloads** (Severity: High)
- **Benzene today:** `ApiGatewayMessageBodyGetter.GetBody` decodes a base64 body as `Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))` whenever `IsBase64Encoded` is set (`ApiGatewayMessageBodyGetter.cs:26`). The whole body contract downstream is `string`.
- **AWS intent:** API Gateway sets `isBase64Encoded=true` precisely for bytes that are *not* text (images, gzip, protobuf, multipart) when the content type is a configured binary media type. Those bytes are not valid UTF-8.
- **Impact:** For a genuine binary upload, `UTF8.GetString` replaces invalid byte sequences with U+FFFD — the original bytes are unrecoverable. The handler can never see a faithful binary request body.
- **Recommendation:** Expose the raw bytes to handlers (a `byte[]`/stream body path); only UTF-8-decode when the negotiated content type is textual.

**[MISSING] Binary/base64 responses cannot be returned — `IsBase64Encoded` is never set on the response** (Severity: High)
- **Benzene today:** `ApiGatewayResponseAdapter.SetBody` only writes `response.Body = body` (a string); nothing ever sets `APIGatewayProxyResponse.IsBase64Encoded` (`ApiGatewayResponseAdapter.cs:52`). No writer of `IsBase64Encoded` on any response.
- **AWS intent:** To return binary content through a proxy integration, the Lambda must base64-encode the body **and** set `isBase64Encoded=true`; API Gateway then decodes it to raw bytes.
- **Impact:** Benzene handlers cannot emit images, PDFs, gzip payloads, or any binary/attachment response. No compression story either.
- **Recommendation:** Add an `IsBase64Encoded` pathway on the response adapter/context and set it when the handler produces binary content.

**[MISSING] `multiValueHeaders` / `multiValueQueryStringParameters` read on neither request nor response — multiple `Set-Cookie` headers impossible** (Severity: Medium)
- **Benzene today:** Request mapping reads only single-value `Headers`/`QueryStringParameters`/`PathParameters`; the multi-value maps are never touched. The response adapter writes only the single-value `Headers` dictionary — `MultiValueHeaders` is never populated. CLAUDE.md documents the request-side drop honestly.
- **AWS intent:** In v1.0, repeated request headers/query keys arrive in the `multiValue*` maps, and the *only* way to emit multiple headers of the same name (notably several `Set-Cookie`) is via response `multiValueHeaders`.
- **Impact:** Repeated inbound headers/query params collapse to the last value; a handler cannot set two cookies or emit repeated `WWW-Authenticate`/`Link` headers. Set-Cookie-heavy auth flows are blocked on v1.
- **Recommendation:** Thread the multi-value maps through the header/query getters; add a `multiValueHeaders` response path (at least for `Set-Cookie`).

**[MISSING] `requestContext` (authorizer claims, identity, stage) not surfaced to handlers** (Severity: Medium)
- **Benzene today:** The raw `APIGatewayProxyRequest` is on the context, but the request-facing surface exposes only path/method/headers/query/path-params. `RequestContext` (authorizer claims/`identity`/`accountId`/`stage`) and `StageVariables` are never mapped.
- **AWS intent:** With a Lambda/Cognito authorizer, downstream handlers read the caller principal/claims from `requestContext.authorizer` / `requestContext.identity`.
- **Impact:** A handler cannot get the authenticated principal or authorizer claims through Benzene's normal request-binding path — it must reach into the raw context, defeating the abstraction. Stage variables inaccessible.
- **Recommendation:** Add an enrichment/adapter path for `requestContext.authorizer`/`identity` claims and `stageVariables` (via the enricher/adapter, not new context marker properties — respecting context purity).

**[MISSING] Lambda authorizer support is v1-IAM-policy only; no HTTP API simple-response ("isAuthorized") authorizer** (Severity: Low)
- **Benzene today:** `AuthorizerExtensions.UseCustomAuthorizer` always produces an `APIGatewayCustomAuthorizerResponse` (full IAM policy document); `CanHandle` keys off `RequestContext.ApiId`.
- **AWS intent:** HTTP API Lambda authorizers support a **simple response** format (`{"isAuthorized": true, "context": {…}}`), the recommended default for HTTP APIs.
- **Impact:** Consistent with the no-v2 stance — HTTP API simple-format authorizers aren't modeled.
- **Recommendation:** Track alongside v2 payload work.

**[MISSING] No ALB target-group event shape, and no Lambda response streaming** (Severity: Low)
- ALB → Lambda uses a near-identical proxy shape (with `multiValueHeaders` and required `statusDescription`); Lambda response streaming is available for large/progressive payloads. Neither is an option. Low.

**[WRONG-APPROACH] Response header casing can produce duplicate content-type entries** (Severity: Low)
- `SetContentType` writes literal lowercase `"content-type"` into a case-sensitive `Dictionary<string,string>`; a handler separately setting `"Content-Type"` yields two distinct keys. Use a case-insensitive comparer for the response `Headers` dictionary.

---

**Verdict:** The v1.0 REST-API text-JSON happy path is solid, but the integration assumes one payload version and a text-only body — HTTP API v2 events fail hard, and binary request/response bodies are corrupted or impossible; these (plus multi-value headers for `Set-Cookie`) are the load-bearing gaps.
