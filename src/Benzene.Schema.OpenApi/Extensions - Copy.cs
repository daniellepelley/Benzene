using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandling;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Core.Results;
using Benzene.Results;
using Newtonsoft.Json;

namespace Benzene.Schema.OpenApi;

public class SpecResponse
{
   public string Content { get; set; } 
}

public class SpecMessageHandler : IMessageHandler<SpecRequest, RawStringMessage>
{
    private readonly IServiceResolver _serviceResolver;

    public SpecMessageHandler(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }
    
    public Task<IServiceResult<RawStringMessage>> HandleAsync(SpecRequest request)
    {
        var output = CreateSpec(_serviceResolver, request ?? new SpecRequest("asyncapi", "json"));

        return ServiceResult.Ok(new RawStringMessage(output)).AsTask();
    }

    private static string CreateSpec(IServiceResolver resolver, SpecRequest specRequest)
    {
        var specBuilder = new SpecBuilder();
        return specBuilder.CreateSpec(resolver, specRequest);
    }
}
