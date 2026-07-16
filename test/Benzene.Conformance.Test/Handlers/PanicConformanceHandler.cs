using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;

namespace Benzene.Conformance.Test.Handlers;

public class PanicRequest
{
}

public class PanicReply
{
}

/// <summary>
/// Canonical mesh conformance handler (docs/specification/conformance/README.md, mesh fixture
/// formats): throws unconditionally, pinning the rule that an unhandled handler exception is traced
/// as <c>ServiceUnavailable</c> rather than lost (mesh.md §3's structural coverage). Registered only
/// for the mesh trace cases, not for descriptor or envelope cases.
/// </summary>
[Message("conformance:panic")]
public class PanicConformanceHandler : IMessageHandler<PanicRequest, PanicReply>
{
    public Task<IBenzeneResult<PanicReply>> HandleAsync(PanicRequest request)
    {
        throw new InvalidOperationException("conformance:panic always throws");
    }
}
