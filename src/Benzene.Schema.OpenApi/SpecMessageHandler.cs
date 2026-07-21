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
        var specRequest = request ?? new SpecRequest("asyncapi", "json");

        // The spec is deterministic for a given (type, format), so serve it from the memoizing
        // SpecCache when one is registered (UseSpec registers a singleton) - only the first request
        // for each combination pays the full schema-generation/serialization cost. Fall back to a
        // direct build if no cache is registered, so behaviour is unchanged when it isn't.
        var cache = _serviceResolver.TryGetService<SpecCache>();
        var output = cache != null
            ? cache.GetOrBuild(specRequest, r => CreateSpec(_serviceResolver, r))
            : CreateSpec(_serviceResolver, specRequest);

        return BenzeneResult.Ok(new RawStringMessage(output)).AsTask();
    }

    private static string CreateSpec(IServiceResolver resolver, SpecRequest specRequest)
    {
        var specBuilder = new SpecBuilder();
        return specBuilder.CreateSpec(resolver, specRequest);
    }
}