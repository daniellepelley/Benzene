using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

namespace Benzene.Integration.Test.Helpers;

[Message(Defaults.Topic)]
public class ExampleMessageHandler : IMessageHandler<ExampleRequestPayload, Benzene.Abstractions.Results.Void>
{
    private readonly IExampleService _exampleService;

    public ExampleMessageHandler(IExampleService exampleService)
    {
        _exampleService = exampleService;
    }

    public Task<IBenzeneResult<Benzene.Abstractions.Results.Void>> HandleAsync(ExampleRequestPayload request)
    {
        _exampleService.Register(request.Name);
        return Task.FromResult(BenzeneResult.Ok(new Benzene.Abstractions.Results.Void()));
    }
}
