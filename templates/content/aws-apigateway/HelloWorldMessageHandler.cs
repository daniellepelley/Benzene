using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

namespace BenzeneStarter;

// This is a starter handler - replace it with your own, or add more alongside it. Your business
// logic lives here, not in Program.cs/StartUp.cs: a handler receives a typed request and returns a
// typed response, and knows nothing about HTTP, Lambda, or queues - the same handler shape runs
// unchanged on every host Benzene supports (see docs/getting-started.md).
//
// [Message("hello:world")] maps this handler to its topic - every Benzene transport routes by
// topic, so this identifier stays constant across HTTP, Lambda, SQS, SNS, and Kafka.
// [HttpEndpoint("GET", "/hello/{name}")] additionally maps an HTTP method and path onto that same
// topic (unused on transports with no HTTP concept, e.g. SQS/SNS/Kafka - harmless to leave in
// place, since the same handler still answers those transports via its topic alone).
[Message("hello:world")]
[HttpEndpoint("GET", "/hello/{name}")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldRequest, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldRequest message)
    {
        var response = new HelloWorldResponse { Message = $"Hello {message.Name}!" };
        return Task.FromResult(BenzeneResult.Ok(response));
    }
}

public class HelloWorldRequest
{
    public string Name { get; set; } = string.Empty;
}

public class HelloWorldResponse
{
    public string Message { get; set; } = string.Empty;
}
