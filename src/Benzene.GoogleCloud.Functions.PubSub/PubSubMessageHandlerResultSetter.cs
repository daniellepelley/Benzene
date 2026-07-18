using Benzene.Core.MessageHandlers;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Records a message handler's outcome onto <see cref="PubSubContext.MessageResult"/>. Cloud
/// Functions Framework has no platform-level per-message acknowledgement to report back to beyond
/// "did <c>HandleAsync</c> throw", but <see cref="PubSubMiddlewareApplication"/> reads this to
/// support <see cref="PubSubOptions.RaiseOnFailureStatus"/>.
/// </summary>
public class PubSubMessageHandlerResultSetter : MessageHandlerResultSetterBase<PubSubContext>;
