using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.Versioning.Response;

/// <summary>
/// Wraps a handler definition, overriding only <see cref="ResponseType"/> with the downcast target
/// type so the inner <c>IResponsePayloadMapper</c> serializes the response as the requested version's
/// CLR shape rather than the handler's canonical response type (docs/specification/versioning.md §4.2).
/// Every other member forwards to the original definition unchanged.
/// </summary>
internal class ResponseTypeOverrideDefinition : IMessageHandlerDefinition
{
    private readonly IMessageHandlerDefinition _inner;

    public ResponseTypeOverrideDefinition(IMessageHandlerDefinition inner, Type responseType)
    {
        _inner = inner;
        ResponseType = responseType;
    }

    public ITopic Topic => _inner.Topic;
    public Type RequestType => _inner.RequestType;
    public Type HandlerType => _inner.HandlerType;
    public Type ResponseType { get; }
}
