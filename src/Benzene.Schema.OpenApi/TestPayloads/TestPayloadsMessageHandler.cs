using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.Messages;
using Benzene.Results;
using Benzene.Schema.OpenApi.EventService;

namespace Benzene.Schema.OpenApi.TestPayloads;

/// <summary>
/// Serves the <c>test-payloads</c> topic: builds the service's own <see cref="EventServiceDocument"/>
/// (the same one the <c>spec</c> topic serves) and returns a manifest of ready-to-fire example
/// payloads for its domain topics, so a deployed service can self-serve valid example calls. Mirrors
/// <see cref="SpecMessageHandler"/>.
/// </summary>
public class TestPayloadsMessageHandler : IMessageHandler<TestPayloadsRequest, RawStringMessage>
{
    private readonly IServiceResolver _serviceResolver;

    public TestPayloadsMessageHandler(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public Task<IBenzeneResult<RawStringMessage>> HandleAsync(TestPayloadsRequest request)
    {
        // Reuse SpecBuilder's runtime document assembly (resolves handler/HTTP/transport finders from
        // DI) - the "benzene" branch returns the populated EventServiceDocumentBuilder.
        var documentBuilder = (EventServiceDocumentBuilder)new SpecBuilder().CreateBuilder(_serviceResolver, "benzene");
        var document = documentBuilder.Build();

        var json = new TestPayloadsBuilder().BuildJson(document, request?.Topic);

        return BenzeneResult.Ok(new RawStringMessage(json)).AsTask();
    }
}
