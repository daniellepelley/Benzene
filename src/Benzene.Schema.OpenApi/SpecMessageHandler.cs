using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.Messages;
using Benzene.Results;

namespace Benzene.Schema.OpenApi;

public class SpecMessageHandler : IMessageHandler<SpecRequest, RawStringMessage>
{
    private readonly IServiceResolver _serviceResolver;

    public SpecMessageHandler(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }
    
    public Task<IBenzeneResult<RawStringMessage>> HandleAsync(SpecRequest request)
    {
        var output = CreateSpec(_serviceResolver, request ?? new SpecRequest("asyncapi", "json"));

        return BenzeneResult.Ok(new RawStringMessage(output)).AsTask();
    }

    private static string CreateSpec(IServiceResolver resolver, SpecRequest specRequest)
    {
        var specBuilder = new SpecBuilder();
        return specBuilder.CreateSpec(resolver, specRequest);
    }
}