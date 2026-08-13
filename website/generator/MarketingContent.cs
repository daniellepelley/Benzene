namespace Benzene.Website.Generator;

/// <summary>
/// The marketing home page's copy, hand-authored for a proper landing-page layout (hero, feature
/// cards, a per-language "get started" selector, platform strip) rather than derived from markdown.
/// Benzene is a language-neutral architecture with several language ports, so this copy leads with
/// the idea and the spec, not any one language.
/// </summary>
internal static class MarketingContent
{
    // The definition line. The claim is deliberately concrete, not categorical: "a framework for
    // message-driven services" names a category and gives no reason to care, so instead it says
    // what you get (the middleware/scoping/testability a modern web framework gives HTTP, on every
    // transport) and the problem it removes (HTTP, SQS, and Kafka normally mean a different service
    // each). Keep this and the docs hub's lede telling the same story — a visitor who meets a
    // different first sentence at each door concludes the site doesn't know what it's selling.
    public const string Tagline =
        "One message handler, every transport. HTTP, SQS, and Kafka are fundamentally different " +
        "things &mdash; normally a different service for each. Benzene gives them all what a " +
        "modern web framework gives HTTP &mdash; one middleware pipeline, per-request scoping, " +
        "handlers you can test &mdash; so you build one service and reach it over every transport " +
        "at once, on the cloud you already run.";

    // The hero's second sentence. Two variants, picked by how many language ports the run actually
    // built: "pick your language below" over a single tab reads as a broken page, and the cold
    // walkthroughs bounced off spec-governance vocabulary ("idiomatic ports", "conformance
    // fixtures") in sentence two of the site — that detail belongs on the spec's own pages, not
    // where a first-time visitor stands. {SPEC} is replaced with the spec home's relative href.
    public const string MultiLanguageLede =
        "One design, <a href=\"{SPEC}\">specified language-neutrally</a> and implemented idiomatically " +
        "per language. Pick yours below &mdash; the concepts are the same in every one.";

    public const string SingleLanguageLede =
        "One design, <a href=\"{SPEC}\">specified language-neutrally</a> and implemented idiomatically " +
        "per language. .NET is the reference implementation; Go, TypeScript, and Python ports are in " +
        "progress and will appear here as they land.";

    public sealed record Feature(string Title, string Body);

    public static readonly Feature[] Features =
    [
        new("Mix transports without the glue",
            "Serverless ties your logic to its trigger &mdash; an SNS function can't also take SQS, and " +
            "putting a queue in front of an HTTP service is bespoke plumbing. A Benzene handler is " +
            "written against a topic, so the same logic is reachable over HTTP, SQS, SNS, Kafka, and " +
            "more at the same time. You add or change a transport in the wiring, never in the handler."),
        new("See what every service does",
            "Handlers, topics, payloads, and validation rules are introspectable. Benzene " +
            "generates OpenAPI and AsyncAPI specs and a live service map straight from your code, " +
            "so your contract and cross-service topology track what actually runs instead of a " +
            "hand-drawn diagram that drifts."),
        new("Test-first, out of the box",
            "Every transport ships a test host and helpers, so you exercise a handler exactly as " +
            "a queue, a function, or HTTP would invoke it: in memory, in a normal unit test, with no " +
            "cloud and no emulator. Mock a dependency, send a message, and assert the result."),
        new("Cross-cutting concerns, once",
            "Correlation IDs, logging, tracing, validation, retries, and health checks are " +
            "composable middleware shared across every transport. Write them once instead of " +
            "scattering them across handlers or re-implementing them per event source."),
    ];

    /// <summary>
    /// One language port's "get started" content for the home-page selector. <see cref="Beta"/>
    /// marks an early port; <see cref="DocsOutputPath"/> is the site-relative output path of that
    /// language's docs home (the generator turns it into a relative href).
    /// </summary>
    public sealed record LanguageStart(
        string Id, string Label, bool Beta, string Install, string Code, string RepoUrl, string DocsOutputPath);

    public static readonly LanguageStart[] Languages =
    [
        new("dotnet", ".NET", false,
            "dotnet add package Benzene.AspNet.Core --prerelease",
            """
            [Message("greet")]
            [HttpEndpoint("POST", "/greet")]
            public class GreetHandler : IMessageHandler&lt;GreetRequest, GreetResponse&gt;
            {
                public Task&lt;IBenzeneResult&lt;GreetResponse&gt;&gt; HandleAsync(GreetRequest message) =&gt;
                    Task.FromResult(BenzeneResult.Ok(new GreetResponse { Greeting = $"Hello, {message.Name}!" }));
            }

            // Wire it onto any host - here ASP.NET Core, in Program.cs:
            builder.Services.UsingBenzene(x =&gt; x.AddMessageHandlers(typeof(GreetHandler).Assembly));
            app.UseBenzene(b =&gt; b.UseHttp(http =&gt; http.UseMessageHandlers()));

            // The same handler from HTTP *and* SQS in one AWS Lambda - change the wiring, never the handler:
            app.UseAwsLambda(aws =&gt; aws.UseApiGateway(api =&gt; api.UseMessageHandlers())
                                       .UseSqs(sqs =&gt; sqs.UseMessageHandlers()));
            """,
            "https://github.com/daniellepelley/benzene-dotnet",
            "dotnet/docs/getting-started.html"),

        new("go", "Go", true,
            "go get github.com/daniellepelley/benzene-go",
            """
            func greetHandler(ctx context.Context, req GreetRequest) benzene.Result[GreetResponse] {
                if req.Name == "" {
                    return benzene.BadRequest[GreetResponse]("name is required")
                }
                return benzene.Ok(GreetResponse{Greeting: "Hello, " + req.Name + "!"})
            }

            // Register it against a topic, reachable over HTTP (and the wire envelope):
            benzene.Register(registry, benzene.NewTopic("greet"),
                benzene.Handler[GreetRequest, GreetResponse](greetHandler))
            """,
            "https://github.com/daniellepelley/benzene-go",
            "go/docs/index.html"),

        new("typescript", "TypeScript", true,
            "npm install @benzene/express @benzene/core-message-handlers @benzene/http @benzene/results",
            """
            @httpEndpoint("POST", "/greet")
            @message("greet", { requestType: GreetRequest, responseType: GreetResponse })
            export class GreetHandler implements IMessageHandler&lt;GreetRequest, GreetResponse&gt; {
              handleAsync(request: GreetRequest): Promise&lt;IBenzeneResultOf&lt;GreetResponse&gt;&gt; {
                return Promise.resolve(BenzeneResult.ok({ greeting: `Hello, ${request.name}!` }));
              }
            }

            // Wire it onto any host - here Express, in index.ts:
            app.use(benzene((pipeline) =&gt; useMessageHandlers(pipeline, GreetHandler)));
            """,
            "https://github.com/daniellepelley/benzene-typescript",
            "typescript/docs/index.html"),

        // Snippet taken from benzene-python's own README rather than written for the site, so the
        // code on this page is something that actually runs against the shipped packages.
        new("python", "Python", true,
            "pip install benzene-core benzene-http",
            """
            from benzene.core import BenzeneMessageApplication, Registry, message
            from benzene.results import Result

            @message("say:hello")
            async def hello(request: dict) -> Result:
                return Result.ok({"greeting": f"Hello {request['name']}"})

            # Wire it into an application - reachable over HTTP via benzene-http:
            app = BenzeneMessageApplication(Registry().add(hello))
            """,
            "https://github.com/daniellepelley/benzene-python",
            "python/docs/index.html"),
    ];

    public static readonly (string Name, string Detail)[] Platforms =
    [
        ("AWS", "Lambda, API Gateway, SQS, SNS, Kafka, EventBridge"),
        ("Azure", "Functions, Event Hub, Service Bus"),
        ("Google Cloud", "Cloud Functions, Cloud Run"),
        ("Cloudflare", "Containers"),
        ("Kubernetes", "Any container host, with liveness/readiness health checks"),
        ("Virtual machines / self-hosted", "An HTTP host or a long-running worker - consumes Kafka, HTTP, or any custom transport"),
    ];
}
