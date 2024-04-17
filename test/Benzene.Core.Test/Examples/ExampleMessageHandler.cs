using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
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

    public Task<IServiceResult<Void>> HandleAsync(ExampleRequestPayload request)
    {
        _exampleService.Register(request.Name);
        return Task.FromResult(ServiceResult.Ok(new Void()));
    }
}


