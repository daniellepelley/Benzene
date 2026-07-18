A message handler in Benzene has no idea if it's running in AWS Lambda, Azure Functions, or a plain ASP.NET Core process. That's the whole point.

Here's one, in full:

[Message("hello:world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldMessage message)
    {
        return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}" }));
    }
}

No SQS event type. No `HttpRequest`. No Function bindings. Just a topic, a request, and a response.

Wiring it into a host is a few lines, and it's the *only* place that host-specific detail lives:

builder.Services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

app.UseBenzene(benzene => benzene
    .UseHttp(http => http
        .UseMessageHandlers()));

Swap `UseHttp` for `UseSqs`, `UseServiceBus`, `UseKafka`, `UseEventHub` — same handler, same tests, same assembly. Handlers are discovered by reflection off the `[Message]` attribute, so there's no routing table to hand-maintain as the service grows.

The pattern is standard hexagonal architecture (ports and adapters) — nothing new in principle. What Benzene adds is a shared middleware pipeline that every adapter runs through, so the stuff that usually gets copy-pasted into every Lambda function or duplicated across Functions and Controllers — correlation IDs, structured logging, request validation, retries, W3C trace context — gets written once and just applies, everywhere, automatically.

The practical effect: your integration tests target the handler, not the transport. Your business logic has zero references to `Amazon.Lambda.*` or `Azure.Messaging.*`. And when the transport changes — because it will — the handler code doesn't.

If you're building event-driven or API services in .NET, this is worth ten minutes: [quickstart link]

#dotnet #csharp #softwarearchitecture #serverless #eventdriven
