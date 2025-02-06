using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Test.Examples;

[HttpEndpoint(Defaults.Method, Defaults.Path)]
[Message(Defaults.Topic)]
public class ExampleMessageHandler : IMessageHandler<ExampleRequestPayload, Void>
{
    private readonly IExampleService _exampleService;

    public ExampleMessageHandler(IExampleService exampleService)
    {
        _exampleService = exampleService;
    }

    public Task<IBenzeneResult<Void>> HandleAsync(ExampleRequestPayload request)
    {
        _exampleService.Register(request.Name);
        return Task.FromResult(BenzeneResult.Ok(new Void()));
    }
}


