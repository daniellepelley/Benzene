using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.S3;

/// <summary>
/// Records the message handler result onto an <see cref="S3RecordContext"/>'s
/// <see cref="S3RecordContext.MessageResult"/>. S3 events are fire-and-forget, so the result is
/// recorded for diagnostics rather than written back to a response.
/// </summary>
public class S3MessageHandlerResultSetter : MessageHandlerResultSetterBase<S3RecordContext>;
