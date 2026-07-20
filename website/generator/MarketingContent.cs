namespace Benzene.Website.Generator;

/// <summary>
/// The marketing home page's copy, hand-authored for a proper landing-page layout (hero, feature
/// cards, platform strip) rather than derived from README.md's markdown - a card grid isn't
/// something plain markdown can express, so this is a deliberately separate, small source of
/// truth kept loosely in sync with README.md's messaging by hand.
/// </summary>
internal static class MarketingContent
{
    public const string Tagline =
        "Write your business logic once and reach it over HTTP, SQS, SNS, Kafka, Event Hub, or " +
        "gRPC &mdash; all at once, on the cloud you already run. Putting a queue in front of an " +
        "HTTP endpoint, or adding a second event source to a worker, takes a line of wiring, not " +
        "a rewrite.";

    public sealed record Feature(string Title, string Body);

    public static readonly Feature[] Features =
    [
        new("Mix transports without the glue",
            "Serverless ties your logic to its trigger. An SNS function can't also take SQS, and " +
            "putting a queue in front of an HTTP service is bespoke plumbing. A Benzene handler is " +
            "plain C# against a topic, so the same logic is reachable over HTTP, SQS, SNS, Kafka, " +
            "and more at the same time. You add or change a transport in the wiring, never in the " +
            "handler."),
        new("See what every service does",
            "Handlers, topics, payloads, and validation rules are introspectable. Benzene " +
            "generates OpenAPI and AsyncAPI specs and a live service map straight from your code, " +
            "so your contract and cross-service topology track what actually runs instead of a " +
            "hand-drawn diagram that drifts."),
        new("Test-first, out of the box",
            "Every transport ships a test host and helpers, so you exercise a handler exactly as " +
            "SQS, Lambda, or HTTP would invoke it: in memory, in a normal unit test, with no cloud " +
            "and no emulator. Mock a dependency, send a message, and assert the result."),
        new("Cross-cutting concerns, once",
            "Correlation IDs, logging, tracing, validation, retries, and health checks are " +
            "composable middleware shared across every transport. Write them once instead of " +
            "scattering them across handlers or re-implementing them per event source."),
    ];

    public sealed record CodeStep(string Label, string Code);

    public static readonly CodeStep[] QuickstartSteps =
    [
        new("A message handler, mapped to a topic",
            """
            [Message("hello:world")]
            [HttpEndpoint("GET", "/hello/{name}")]
            public class HelloWorldMessageHandler : IMessageHandler&lt;HelloWorldRequest, HelloWorldResponse&gt;
            {
                public Task&lt;IBenzeneResult&lt;HelloWorldResponse&gt;&gt; HandleAsync(HelloWorldRequest message)
                {
                    return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}" }));
                }
            }
            """),
        new("Hosted as one AWS Lambda, reached over four transports",
            """
            public class StartUp : BenzeneStartUp
            {
                public override void ConfigureServices(IServiceCollection services, IConfiguration config) =&gt;
                    services.UsingBenzene(x =&gt; x.AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

                public override void Configure(IBenzeneApplicationBuilder app, IConfiguration config) =&gt;
                    app.UseAwsLambda(aws =&gt;
                    {
                        // One function, the same handler reached four ways:
                        aws.UseApiGateway(http =&gt; http.UseMessageHandlers());       // HTTP
                        aws.UseSqs(sqs =&gt; sqs.UseMessageHandlers());                // SQS queue
                        aws.UseSns(sns =&gt; sns.UseMessageHandlers());                // SNS topic
                        aws.UseEventBridge(events =&gt; events.UseMessageHandlers());  // EventBridge
                    });
            }

            public class Function : AwsLambdaHost&lt;StartUp&gt;;
            """),
    ];

    public static readonly (string Name, string Detail)[] Platforms =
    [
        ("AWS", "Lambda, API Gateway, SQS, SNS, Kafka, EventBridge"),
        ("Azure", "Functions (isolated worker), Event Hub, Service Bus"),
        ("Google Cloud", "Cloud Functions, Cloud Run"),
        ("Cloudflare", "Containers"),
        ("Kubernetes", "Any container host, with liveness/readiness health checks"),
        ("Virtual machines / self-hosted", "ASP.NET Core or a long-running worker - consumes Kafka, HTTP, or any custom transport"),
    ];

    public const string InstallCommand = "dotnet add package Benzene.Aws.Lambda.ApiGateway --prerelease";
}
