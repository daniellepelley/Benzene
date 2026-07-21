using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Example.Asp.Minimal;

// This is where your logic lives - and the only file you'd carry over verbatim if you later
// moved this handler to AWS Lambda or Azure Functions. It knows nothing about HTTP.
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
    public string Name { get; set; }
}

public class HelloWorldResponse
{
    public string Message { get; set; }
}
