namespace Benzene.Website.Generator;

/// <summary>
/// Content for the hand-authored value-themed marketing sub-pages (Why Benzene / Architecture /
/// Operations), rendered by <see cref="Layout.RenderValuePage"/>. Deliberately data-driven and
/// separate from <see cref="MarketingContent"/> (the home page) so each page is one small source of
/// truth. Every claim links to a real docs page or live demo; anything partial or pre-1.0 is stated
/// as such, per <c>work/website-marketing-aims.md</c>'s honesty gates and
/// <c>work/website-audience-plan.md</c>.
/// </summary>
internal static class MarketingPages
{
    /// <summary>One feature card. <paramref name="BodyHtml"/> is raw HTML (may contain links/code).</summary>
    internal sealed record Card(string Title, string BodyHtml);

    /// <summary>A page section: a heading, a lede paragraph (raw HTML), and a grid of cards.</summary>
    internal sealed record Section(string Heading, string LedeHtml, Card[] Cards);

    /// <summary>A whole value page. <paramref name="Slug"/> is the root-level output file (e.g. "why.html").</summary>
    internal sealed record ValuePage(
        string Slug, string NavTitle, string Title, string Description, string HeroLedeHtml,
        Section[] Sections, string CtaHtml);

    public static readonly ValuePage[] All = [Why(), Architecture(), Operations()];

    private static ValuePage Why() => new(
        Slug: "why.html",
        NavTitle: "Why Benzene",
        Title: "Why Benzene",
        Description: "The case for Benzene: lower the cost of change, reduce lock-in and risk, ship "
            + "reliable services your team can test, on infrastructure you already run.",
        HeroLedeHtml:
            "Adopting a framework is a bet on cost, risk, and longevity as much as on ergonomics. "
            + "Benzene's bet is simple: your business logic is the asset, and everything else &mdash; "
            + "the cloud, the transport, the host &mdash; is a detail that should be cheap to change.",
        Sections:
        [
            new Section("Lower the cost of change",
                "Requirements move. New event source, a queue in front of an API, a service split in two. "
                + "In most stacks each of those is a rewrite; in Benzene it's wiring.",
            [
                new Card("The handler doesn't change",
                    "Your logic is a plain C# <a href=\"docs/message-handlers.html\">message handler</a> "
                    + "against a topic. Reaching it over a new transport &mdash; HTTP, a queue, a topic, an "
                    + "event bus &mdash; is a line in the host wiring, not a change to the code that was "
                    + "already written and tested."),
                new Card("Fewer moving parts to run",
                    "One function can serve HTTP <em>and</em> several queues and topics at once, so a service "
                    + "is one deployable, not a sprawl of single-purpose functions &mdash; less to deploy, "
                    + "monitor, and pay for."),
                new Card("Evolve without a rewrite",
                    "Put a queue in front of a synchronous endpoint, or promote a response into a published "
                    + "event, by adding middleware &mdash; the kind of change that normally means re-plumbing "
                    + "a service."),
            ]),
            new Section("Reduce lock-in and risk",
                "The expensive kind of lock-in isn't which cloud you're on &mdash; it's how deeply your code "
                + "is tangled into one vendor's event model. Benzene keeps that boundary thin.",
            [
                new Card("Transport- and vendor-agnostic",
                    "Handlers, middleware, and topics don't know whether they're behind Lambda, Azure "
                    + "Functions, a self-hosted worker, or ASP.NET Core. Moving is a "
                    + "<a href=\"docs/hosting.html\">hosting change</a>, and the SDK is never hidden from you "
                    + "when you need it."),
                new Card("Plain C#, no proprietary runtime",
                    "It's the .NET, dependency injection, and <code>async</code> your team already knows &mdash; "
                    + "no bespoke DSL or engine to learn or be trapped by. Onboarding is a normal C# "
                    + "onboarding."),
                new Card("Open, and yours to fork",
                    "MIT-licensed and open source. No per-seat fee, no telemetry phone-home, no vendor whose "
                    + "roadmap you're hostage to."),
            ]),
            new Section("Quality and reliability you can point to",
                "Two of the biggest drivers of long-run cost are defects that reach production and incidents "
                + "no one can diagnose. Benzene is built to keep both down.",
            [
                new Card("Test-first, without the cloud",
                    "Every transport ships a <a href=\"docs/testing-benzene.html\">test host and helpers</a>, so "
                    + "a handler is exercised exactly as SQS, Lambda, or HTTP would invoke it &mdash; in memory, "
                    + "no emulator, in a normal unit test. Quality is built in, not bolted on."),
                new Card("Observable by construction",
                    "Correlation IDs, structured logs, metrics, and distributed traces come from composable "
                    + "middleware shared across transports &mdash; see <a href=\"operations.html\">Operations</a> "
                    + "for the full picture."),
                new Card("Reliable under real traffic",
                    "Health checks, retries, idempotency, and per-message failure handling are first-class, not "
                    + "afterthoughts. The <a href=\"operations.html\">Operations</a> page lays out exactly what's "
                    + "in the box &mdash; and, honestly, what you supply yourself."),
            ]),
            new Section("Built to last",
                "A framework you build on should be predictable to upgrade and honest about where it is.",
            [
                new Card("A versioning policy, not surprises",
                    "Semantic versioning with written <a href=\"docs/migration-alpha-to-1.0.html\">migration "
                    + "guides</a> for breaking changes, so upgrades are planned, not archaeology."),
                new Card("Modern .NET, broad reach",
                    "Built on .NET 10, with core packages also targeting older runtimes for back-compat. Runs "
                    + "on the hosts your platform team already chose &mdash; see "
                    + "<a href=\"architecture.html\">Architecture</a>."),
                new Card("Pre-1.0, and candid about it",
                    "Benzene is approaching 1.0; packages are published as prerelease (<code>--prerelease</code>) "
                    + "today. The docs mark anything partial or planned as such rather than overselling it."),
            ]),
        ],
        CtaHtml:
            "<a class=\"button\" href=\"docs/getting-started.html\">Get started</a> "
            + "<a class=\"button button-secondary\" href=\"architecture.html\">See the architecture</a>");

    private static ValuePage Architecture() => new(
        Slug: "architecture.html",
        NavTitle: "Architecture",
        Title: "Architecture",
        Description: "Benzene is a hexagonal (ports-and-adapters) framework: message handlers are the "
            + "core, transports are adapters, and one middleware pipeline runs through all of them. "
            + "Designed to fit your system, not replace it.",
        HeroLedeHtml:
            "Benzene separates <em>what your service does</em> from <em>how it's invoked</em>. A handler "
            + "holds your logic; a transport turns a native request into a message and routes it through a "
            + "shared middleware pipeline. That one idea &mdash; ports and adapters, applied honestly &mdash; "
            + "is where every other property on this page comes from.",
        Sections:
        [
            new Section("Ports and adapters, applied honestly",
                "The <a href=\"docs/specification/design-principles.html\">design principles</a> are explicit "
                + "and the <a href=\"docs/specification/core-concepts.html\">core concepts</a> are small.",
            [
                new Card("Handlers are the core",
                    "A <a href=\"docs/message-handlers.html\">message handler</a> is "
                    + "<code>IMessageHandler&lt;TRequest, TResponse&gt;</code> &mdash; ordinary C# that takes a "
                    + "typed request and returns a typed result. It has no idea what transport carried it."),
                new Card("Transports are adapters",
                    "HTTP, SQS, SNS, Kafka, EventBridge, Event Hub, Service Bus, gRPC &mdash; each is an adapter "
                    + "that maps a native event onto a message and back. Adding a vendor means writing an "
                    + "adapter, never touching the core."),
                new Card("One pipeline through all of them",
                    "Every adapter runs the same composable <a href=\"docs/middleware.html\">middleware "
                    + "pipeline</a>, so cross-cutting behavior is written once and applies everywhere &mdash; not "
                    + "re-implemented per event source."),
            ]),
            new Section("One model, many transports &mdash; at once",
                "The differentiator isn't swapping clouds; it's that the same handler is reachable over many "
                + "transports simultaneously, on the cloud you already run.",
            [
                new Card("Mix transports in one service",
                    "Expose a handler over HTTP for callers and over a queue for async work in the same host, "
                    + "with one line of wiring each. On serverless that normally means separate, "
                    + "trigger-locked functions."),
                new Card("Host it unchanged",
                    "The same <code>BenzeneStartUp</code> runs on <a href=\"docs/hosting.html\">AWS Lambda, "
                    + "Azure Functions, a self-hosted worker, or ASP.NET Core</a> &mdash; \"three ways Benzene "
                    + "starts\", one application definition."),
                new Card("Reach the native context when you must",
                    "Abstractions don't trap you: <code>IBenzeneInvocation</code> and typed features are the "
                    + "escape hatch to the underlying platform object when a handler genuinely needs it."),
            ]),
            new Section("Introspectable by design",
                "A Benzene service can describe itself &mdash; its topics, payloads, and contracts &mdash; from "
                + "the same code that handles messages. No hand-maintained diagram drifts out of date.",
            [
                new Card("OpenAPI + AsyncAPI from your code",
                    "<a href=\"docs/spec.html\">Spec generation</a> derives OpenAPI (HTTP) and AsyncAPI (events) "
                    + "documents from the handler registry, with example payloads and validation rules included."),
                new Card("Browse it in the Spec UI",
                    "A self-contained <a href=\"docs/spec-ui.html\">Spec UI</a> renders that contract &mdash; "
                    + "topics, payload tables, validation chips, transport chips. "
                    + "<a href=\"demos/spec/index.html\">Open the live demo &rarr;</a>"),
                new Card("A map across services",
                    "Aggregate every service's contract and health into a searchable service map with "
                    + "contract-drift detection and cross-service topology. "
                    + "<a href=\"demos/mesh/index.html\">Open the live demo &rarr;</a> (the mesh tooling is "
                    + "shipped and evolving.)"),
            ]),
            new Section("Composable, swappable, standards-friendly",
                "The seams are deliberate, so Benzene slots into your stack rather than dictating it.",
            [
                new Card("Your DI, your serializers",
                    "Microsoft DI by default, with an Autofac option; System.Text.Json by default, with "
                    + "Newtonsoft, XML, Avro, and MessagePack available &mdash; chosen per content type, "
                    + "swappable by registration."),
                new Card("Middleware is the extension point",
                    "Auth, validation, resilience, logging, idempotency &mdash; each is middleware you add, "
                    + "reorder, or replace. Your own concerns plug in the same way."),
                new Card("Interop on open envelopes",
                    "A transport-agnostic wire envelope (topic, headers, body) and standards-based specs let "
                    + "Benzene services interoperate across vendors and with non-Benzene consumers."),
            ]),
        ],
        CtaHtml:
            "<a class=\"button\" href=\"docs/index.html\">Read the docs</a> "
            + "<a class=\"button button-secondary\" href=\"operations.html\">Run it in production &rarr;</a>");

    private static ValuePage Operations() => new(
        Slug: "operations.html",
        NavTitle: "Operations",
        Title: "Operations",
        Description: "What it takes to run Benzene in production: observability (OpenTelemetry traces, "
            + "metrics, structured logs), health checks, failure handling (retries, idempotency, "
            + "partial-batch failures), and deployment across hosts.",
        HeroLedeHtml:
            "A service is only as good as your ability to see it and trust it under load. Benzene's "
            + "operational features are ordinary middleware &mdash; the same on every transport &mdash; and "
            + "the docs are candid about what's in the box versus what you supply.",
        Sections:
        [
            new Section("See everything it's doing",
                "Observability is opt-in middleware, shared across transports, and free until you attach an "
                + "exporter. Full picture in <a href=\"docs/monitoring.html\">Monitoring</a>.",
            [
                new Card("Traces and metrics via OpenTelemetry",
                    "Every middleware in every pipeline can emit its own <code>Activity</code> span, plus "
                    + "processed-count and duration metrics. Export to Jaeger, Tempo, Prometheus, or App "
                    + "Insights through standard <a href=\"docs/cookbooks/distributed-tracing-opentelemetry.html\">"
                    + "OpenTelemetry</a> &mdash; Benzene stays exporter-agnostic."),
                new Card("Traces that cross service hops",
                    "<code>UseW3CTraceContext()</code> continues a W3C trace from inbound headers and stamps it "
                    + "onto outbound messages, so one request is one trace across HTTP and queue/topic hops "
                    + "(SQS, SNS, Kafka, Event Hub)."),
                new Card("Structured, correlated logs",
                    "One <code>UseBenzeneEnrichment()</code> attaches invocation id, trace/span id, topic, "
                    + "transport, and handler to the log scope on every platform &mdash; through standard "
                    + "<code>Microsoft.Extensions.Logging</code>, so Serilog or App Insights just work."),
            ]),
            new Section("Know when it's healthy",
                "Health is pipeline middleware, so it works on every transport, not just HTTP. See "
                + "<a href=\"docs/health-checks.html\">Health Checks</a>.",
            [
                new Card("Liveness and readiness",
                    "Kubernetes-shaped <a href=\"docs/kubernetes-health-checks.html\">liveness/readiness "
                    + "probes</a> &mdash; on a topic for any transport, or on <code>GET /livez</code> / "
                    + "<code>/readyz</code> for HTTP hosts."),
                new Card("Dependency checks included",
                    "Ship-ready checks for EF Core database connectivity/migrations, a downstream HTTP ping, "
                    + "and schema availability &mdash; wired as readiness checks."),
                new Card("Aggregated, with timeouts",
                    "Checks run together and report an aggregated status; each is wrapped in a timeout so one "
                    + "slow dependency can't hang the probe (a fixed 10s guard today)."),
            ]),
            new Section("Fail safely",
                "At-least-once transports redeliver; batches partially fail; dependencies wobble. Benzene gives "
                + "you first-class handling for each &mdash; and the "
                + "<a href=\"docs/capability-matrix.html\">capability matrix</a> is the honest what-does-what.",
            [
                new Card("Retries, and full resilience via Polly",
                    "Retry with exponential backoff is built in (<code>UseRetry()</code>). For circuit breakers, "
                    + "timeouts, hedging, and fallback, Benzene runs <em>your</em> "
                    + "<a href=\"docs/cookbooks/polly-resilience.html\">Polly pipeline</a> as middleware &mdash; "
                    + "exposing Polly rather than re-abstracting it."),
                new Card("Idempotency for redelivery",
                    "<a href=\"docs/cookbooks/idempotency.html\">Idempotency middleware</a> makes a handler run "
                    + "at most once per message via a pluggable store. In-memory out of the box; back it with "
                    + "DynamoDB or Redis for multi-instance deployments."),
                new Card("Partial-batch failure, done right",
                    "On SQS, only the messages that actually failed are reported back for redelivery &mdash; not "
                    + "the whole batch. Per-transport ack/nack and failure escalation are covered in "
                    + "<a href=\"docs/cookbooks/handling-sqs-failures.html\">Handling SQS failures</a>. (Dead-letter "
                    + "queues remain your infrastructure config.)"),
            ]),
            new Section("Ship it where you run",
                "Deployment is a wiring choice, not a rewrite &mdash; the hosting reach is in "
                + "<a href=\"docs/hosting.html\">Hosting</a>.",
            [
                new Card("Serverless, containers, or bare process",
                    "AWS Lambda and Azure Functions for serverless; ASP.NET Core or a self-hosted worker for "
                    + "containers, Kubernetes, and VMs &mdash; the same handlers, unchanged."),
                new Card("Infrastructure as code",
                    "Generate <a href=\"docs/terraform.html\">Terraform</a> for the Lambda and its event-source "
                    + "permissions straight from your handlers, so the deployment surface tracks the code."),
                new Card("Tuned for cold starts",
                    "A Roslyn source generator can discover handlers at compile time instead of by reflection, "
                    + "one of several <a href=\"docs/cookbooks/lambda-cold-start-optimization.html\">cold-start "
                    + "optimizations</a> for serverless."),
            ]),
        ],
        CtaHtml:
            "<a class=\"button\" href=\"docs/capability-matrix.html\">See the capability matrix</a> "
            + "<a class=\"button button-secondary\" href=\"why.html\">Why Benzene &rarr;</a>");
}
