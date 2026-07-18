using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Records the message handler result onto an <see cref="SnsRecordContext"/>'s
/// <see cref="SnsRecordContext.MessageResult"/>.
/// </summary>
public class SnsMessageHandlerResultSetter : MessageHandlerResultSetterBase<SnsRecordContext>;
