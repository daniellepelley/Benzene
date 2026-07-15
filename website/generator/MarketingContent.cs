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
        "One middleware pipeline. Every cloud. Write your message handlers once, then run the " +
        "same service behind AWS Lambda, Azure Functions, Cloudflare, Kafka, or a plain ASP.NET " +
        "Core host &mdash; without rewriting any of it.";

    public sealed record Feature(string Title, string Body);

    public static readonly Feature[] Features =
    [
        new("Write once, deploy anywhere",
            "A message handler is plain C# against a topic. Swap the transport &mdash; HTTP, " +
            "Lambda, SQS, Kafka &mdash; without touching handler code."),
        new("Cross-cutting concerns, once",
            "Correlation IDs, logging, validation, and health checks live in composable " +
            "middleware, not scattered across every handler."),
        new("No routing tables to maintain",
            "Handlers are discovered automatically by reflection and mapped to their topic and " +
            "HTTP route by attribute, so there's nothing to wire up by hand."),
        new("Multi-cloud by design",
            "The same handlers run on AWS, Azure, Google Cloud, Cloudflare, or a plain ASP.NET " +
            "Core host &mdash; pick per-service, change your mind later."),
    ];

    public sealed record CodeStep(string Label, string Code);

    public static readonly CodeStep[] QuickstartSteps =
    [
        new("A message handler, mapped to a topic",
            """
            [Message("hello:world")]
            public class HelloWorldMessageHandler : IMessageHandler&lt;HelloWorldMessage, HelloWorldResponse&gt;
            {
                public Task&lt;IBenzeneResult&lt;HelloWorldResponse&gt;&gt; HandleAsync(HelloWorldMessage message)
                {
                    return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}" }));
                }
            }
            """),
        new("Wired into an ASP.NET Core host",
            """
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.UsingBenzene(x =&gt; x
                .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

            var app = builder.Build();

            app.UseBenzene(benzene =&gt; benzene
                .UseHttp(http =&gt; http
                    .UseMessageHandlers()));

            app.Run();
            """),
    ];

    public static readonly (string Name, string Detail)[] Platforms =
    [
        ("AWS", "Lambda, API Gateway, SQS, SNS, Kafka, EventBridge"),
        ("Azure", "Functions (isolated worker), Event Hub, Service Bus"),
        ("Google Cloud", "Cloud Functions, Cloud Run"),
        ("Cloudflare", "Containers"),
        ("Kafka", "Self-hosted worker, ordered per partition"),
        ("ASP.NET Core", "Embedded in any existing host"),
    ];

    public const string InstallCommand = "dotnet add package Benzene.AspNet.Core --prerelease";
}
