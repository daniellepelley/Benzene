using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Messages.BenzeneMessage;

public class BenzeneMessageContext : IHasMessageResult
{
    public BenzeneMessageContext(IBenzeneMessageRequest benzeneMessageRequest)
    {
        BenzeneMessageRequest = benzeneMessageRequest;
        BenzeneMessageResponse = new BenzeneMessageResponse();
    }

    public IBenzeneMessageRequest BenzeneMessageRequest { get; }
    public IBenzeneMessageResponse BenzeneMessageResponse { get; set; }

    /// <summary>
    /// The outcome of handling this message, recorded by <c>BenzeneMessageHandlerResultSetter</c> so a
    /// cross-cutting observer of the completed pipeline (e.g. metrics) sees a real success/failure signal.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;
}
