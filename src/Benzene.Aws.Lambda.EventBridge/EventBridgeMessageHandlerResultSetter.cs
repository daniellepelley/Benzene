using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Records the handler outcome on the context. EventBridge target invocations are fire-and-forget,
/// so — like SNS — there is no response body to write, only the message result.
/// </summary>
public class EventBridgeMessageHandlerResultSetter : MessageHandlerResultSetterBase<EventBridgeContext>;
